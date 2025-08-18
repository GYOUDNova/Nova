<#
.SYNOPSIS
Packs the Unity package into Packages/<PackageName>, bumps version, and pushes a release branch (and optional tag).

.DESCRIPTION
- Copies selected Assets/* folders into Packages/<PackageName>.
- Generates .meta files for folders/text so Unity won't complain in immutable Packages/.
- Writes a package-local .gitattributes to avoid Git LFS/text filters breaking binaries.
- Creates/updates a release branch (default: release-v<Version>) and optionally an annotated tag v<Version>.
- Stages only Packages/<PackageName>.
#>

param(
  [Parameter(Mandatory=$true)][string]$Version,

  [string]$BaseBranch = "main",
  [string]$BranchName = "",          # default: release-v<Version>
  [switch]$ForceBranch,              # push with --force-with-lease
  [switch]$TagRelease,               # also create/push tag v<Version>

  # package.json defaults (used when creating a new one or when -OverrideCoreMetadata is set)
  [string]$PackageName    = "com.gyoudnova.handrecognition",
  [string]$DisplayName    = "Hand Recognition",
  [string]$Description    = "This is the package for hand recognition project created by Gyoud Nova",
  [string]$UnityVersion   = "6000.0",
  [string]$UnityRelease   = "39f1",
  [string]$AuthorName     = "Gyoud Nova",

  [switch]$OverrideCoreMetadata,     # overwrite name/display/unity/description in package.json
  [switch]$RebuildSamples            # rebuild samples[] from Samples~ subfolders
)

# -------------------- Setup --------------------

$ErrorActionPreference = "Stop"

# Accept "v1.2.3" or "1.2.3"
if ($Version -match '^[vV](.+)$') { $Version = $Matches[1] }

# Keep branch distinct from tag to avoid ambiguity
if ([string]::IsNullOrWhiteSpace($BranchName)) { $BranchName = "release-v$Version" }

function Require($name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    throw "Missing dependency in PATH: $name"
  }
}
Require git

# Resolve repo root (works from any subfolder)
$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = & git -C $ScriptDir rev-parse --show-toplevel 2>$null
if (-not $ProjectRoot) { $ProjectRoot = (Resolve-Path $ScriptDir).Path }

# -------------------- Create/reset release branch --------------------

Push-Location $ProjectRoot

# Require a clean working tree (stash or commit first if needed)
if (git status --porcelain) {
  Pop-Location
  throw "Working tree is not clean."
}

git fetch origin | Out-Null
git checkout $BaseBranch | Out-Null
git pull --ff-only | Out-Null

$remoteExists = -not [string]::IsNullOrWhiteSpace((git ls-remote --heads origin $BranchName))
if ($remoteExists -and -not $ForceBranch) {
  Pop-Location
  throw "Remote branch '$BranchName' already exists. Re-run with -ForceBranch to update it."
}

if ((git branch --list $BranchName)) { git branch -D $BranchName | Out-Null }
git checkout -B $BranchName $BaseBranch | Out-Null

Pop-Location

# -------------------- Paths --------------------

$PackagesRoot = Join-Path $ProjectRoot "Packages"
$PackagePath  = Join-Path $PackagesRoot $PackageName

$Assets               = Join-Path $ProjectRoot "Assets"
$Src_Scripts          = Join-Path $Assets "Scripts"
$Src_Images           = Join-Path $Assets "Images"
$Src_Prefabs          = Join-Path $Assets "Prefabs"
$Src_Samples          = Join-Path $Assets "Samples"
$Src_Streaming        = Join-Path $Assets "StreamingAssets"
$Src_Tests            = Join-Path $Assets "Tests"
$Src_UIToolkit        = Join-Path $Assets "UI Toolkit"

$Dst_Editor           = Join-Path $PackagePath "Editor"
$Dst_Images           = Join-Path $PackagePath "Images"
$Dst_Runtime          = Join-Path $PackagePath "Runtime"
$Dst_Runtime_Prefabs  = Join-Path $Dst_Runtime "Prefabs"
$Dst_Samples          = Join-Path $PackagePath "Samples~"       # hidden until user imports
$Dst_Streaming        = Join-Path $PackagePath "StreamingAssets"
$Dst_Tests            = Join-Path $PackagePath "Tests"
$Dst_UIToolkit        = Join-Path $PackagePath "UI Toolkit"

# -------------------- Helpers --------------------

function New-Dir([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
  }
}

function Clear-Dir([string]$Path) {
  if (Test-Path -LiteralPath $Path) {
    Get-ChildItem -LiteralPath $Path -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
  } else {
    New-Dir $Path
  }
}

function New-GuidHex { ([guid]::NewGuid().ToString("N")) }

<#
.SYNOPSIS
Create a .meta file for a folder if missing.
#>
function Write-FolderMeta([string]$FolderPath) {
  $metaPath = "$FolderPath.meta"
  if (Test-Path -LiteralPath $metaPath) { return }
  $guid = New-GuidHex
  @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Out-File -FilePath $metaPath -Encoding ascii
}

<#
.SYNOPSIS
Create a .meta file for a text asset if missing (README, package.json, etc.).
#>
function Write-TextMeta([string]$FilePath) {
  $metaPath = "$FilePath.meta"
  if (Test-Path -LiteralPath $metaPath) { return }
  $guid = New-GuidHex
  @"
fileFormatVersion: 2
guid: $guid
TextScriptImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@ | Out-File -FilePath $metaPath -Encoding ascii
}

<#
.SYNOPSIS
Copy folder root .meta if available (preserves GUID), else synthesize a new one.
#>
function Copy-Or-Make-FolderMeta([string]$SrcFolder, [string]$DstFolder) {
  $srcParent = Split-Path -Parent $SrcFolder
  $srcLeaf   = Split-Path -Leaf $SrcFolder
  $srcMeta   = Join-Path $srcParent ($srcLeaf + ".meta")
  $dstMeta   = "$DstFolder.meta"

  if (Test-Path -LiteralPath $srcMeta) {
    Copy-Item -LiteralPath $srcMeta -Destination $dstMeta -Force
  } else {
    Write-FolderMeta $DstFolder
  }
}

<#
.SYNOPSIS
Copy the contents of a folder, excluding specific child names and their .meta.
#>
function Copy-DirContents {
  param(
    [Parameter(Mandatory=$true)][string]$From,
    [Parameter(Mandatory=$true)][string]$To,
    [string[]]$ExcludeChildNames = @()
  )

  if (-not (Test-Path -LiteralPath $From -PathType Container)) { return }
  New-Dir $To

  Get-ChildItem -LiteralPath $From -Force | ForEach-Object {
    if ($ExcludeChildNames -contains $_.Name) { return }

    if ($_.Extension -ieq ".meta") {
      $base = [IO.Path]::GetFileNameWithoutExtension($_.Name)
      if ($ExcludeChildNames -contains $base) { return }
    }

    $dest = Join-Path $To $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force -ErrorAction Stop
  }
}

<#
.SYNOPSIS
Copy an entire directory tree's contents into a destination folder.
#>
function Copy-TreeTo {
  param(
    [Parameter(Mandatory=$true)][string]$From,
    [Parameter(Mandatory=$true)][string]$To
  )
  if (-not (Test-Path -LiteralPath $From -PathType Container)) { return }
  New-Dir $To

  Get-ChildItem -LiteralPath $From -Force | ForEach-Object {
    $dest = Join-Path $To $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force -ErrorAction Stop
  }
}

# -------------------- Build package tree --------------------

New-Dir $PackagesRoot
New-Dir $PackagePath

# Start clean inside the package
Clear-Dir $Dst_Editor
Clear-Dir $Dst_Images
Clear-Dir $Dst_Runtime
Clear-Dir $Dst_Samples
Clear-Dir $Dst_Streaming
Clear-Dir $Dst_Tests
Clear-Dir $Dst_UIToolkit

# Editor <- Assets/Scripts  (excluding Dev Utilities)
Copy-DirContents -From $Src_Scripts -To $Dst_Editor -ExcludeChildNames @("Dev Utilities")
Copy-Or-Make-FolderMeta -SrcFolder $Src_Scripts -DstFolder $Dst_Editor

# Images <- Assets/Images
Copy-TreeTo -From $Src_Images -To $Dst_Images
Copy-Or-Make-FolderMeta -SrcFolder $Src_Images -DstFolder $Dst_Images

# Runtime/Prefabs <- Assets/Prefabs
New-Dir $Dst_Runtime
Write-FolderMeta $Dst_Runtime
if (Test-Path -LiteralPath $Src_Prefabs) {
  Copy-TreeTo -From $Src_Prefabs -To $Dst_Runtime_Prefabs
  Copy-Or-Make-FolderMeta -SrcFolder $Src_Prefabs -DstFolder $Dst_Runtime_Prefabs
}

# Samples~ <- Assets/Samples  (no Samples~.meta on purpose)
Copy-TreeTo -From $Src_Samples -To $Dst_Samples

# StreamingAssets <- Assets/StreamingAssets
Copy-TreeTo -From $Src_Streaming -To $Dst_Streaming
Copy-Or-Make-FolderMeta -SrcFolder $Src_Streaming -DstFolder $Dst_Streaming

# Tests <- Assets/Tests
Copy-TreeTo -From $Src_Tests -To $Dst_Tests
Copy-Or-Make-FolderMeta -SrcFolder $Src_Tests -DstFolder $Dst_Tests

# UI Toolkit <- Assets/UI Toolkit
Copy-TreeTo -From $Src_UIToolkit -To $Dst_UIToolkit
Copy-Or-Make-FolderMeta -SrcFolder $Src_UIToolkit -DstFolder $Dst_UIToolkit

# Remove stray meta for excluded folder if present
$DevUtilMeta = Join-Path $Dst_Editor "Dev Utilities.meta"
Remove-Item -LiteralPath $DevUtilMeta -Force -ErrorAction SilentlyContinue

# -------------------- Docs & .gitattributes --------------------

$ReadmePath     = Join-Path $PackagePath "README.md"
$LicensePath    = Join-Path $PackagePath "LICENSE"

if (Test-Path (Join-Path $ProjectRoot "README.md"))     { Copy-Item (Join-Path $ProjectRoot "README.md")     $ReadmePath     -Force }
if (Test-Path (Join-Path $ProjectRoot "LICENSE"))    { Copy-Item (Join-Path $ProjectRoot "LICENSE")    $LicensePath    -Force }

foreach ($p in @($ReadmePath,$LicensePath)) {
  if (Test-Path -LiteralPath $p) { Write-TextMeta $p }
}

# Disable LFS/text filters for files inside the package
$PkgGitAttr = Join-Path $PackagePath ".gitattributes"
@"
* -filter -diff -merge -text
*.png   -filter -diff -merge -text
*.jpg   -filter -diff -merge -text
*.jpeg  -filter -diff -merge -text
*.psd   -filter -diff -merge -text
*.tga   -filter -diff -merge -text
*.bmp   -filter -diff -merge -text
*.exr   -filter -diff -merge -text
*.wav   -filter -diff -merge -text
*.mp3   -filter -diff -merge -text
*.mp4   -filter -diff -merge -text
*.prefab -filter -diff -merge -text
*.mat   -filter -diff -merge -text
*.anim  -filter -diff -merge -text
*.controller -filter -diff -merge -text
"@ | Out-File -Encoding ASCII $PkgGitAttr
Write-TextMeta $PkgGitAttr

# -------------------- package.json --------------------

$PkgJsonPath = Join-Path $PackagePath "package.json"

if (Test-Path -LiteralPath $PkgJsonPath) {
  $pkg = Get-Content -LiteralPath $PkgJsonPath -Raw | ConvertFrom-Json
} else {
  $pkg = [pscustomobject]@{
    name        = $PackageName
    version     = $Version
    displayName = $DisplayName
    description = $Description
    unity       = $UnityVersion
    unityRelease= $UnityRelease
    keywords    = @("Webcam","Hand Gesture","Sign language")
    author      = @{ name = $AuthorName }
    dependencies= @{
      "com.gilzoide.sqlite-net"      = "1.2.3"
      "com.github.homuler.mediapipe" = "0.16.1"
      "com.unity.ugui"               = "2.0.0"
      "com.unity.editorcoroutines"   = "1.0.0"
    }
    samples     = @(
      @{ displayName="Sample Menu UI";  description="Contains a sample scene and scripts for a menu UI"; path="Samples~/SampleMenu" },
      @{ displayName="Sample RollABall"; description="Contains a sample RollABall scene and related scripts"; path="Samples~/Rollaball" }
    )
  }
}

# Bump version
$pkg.version = $Version

# Optionally refresh core fields
if ($OverrideCoreMetadata -or -not (Test-Path -LiteralPath $PkgJsonPath)) {
  $pkg.name         = $PackageName
  $pkg.displayName  = $DisplayName
  $pkg.description  = $Description
  $pkg.unity        = $UnityVersion
  $pkg.unityRelease = $UnityRelease
  if (-not $pkg.author) { $pkg | Add-Member -NotePropertyName author -NotePropertyValue @{ name = $AuthorName } }
  elseif (-not $pkg.author.name) { $pkg.author.name = $AuthorName }
}

# Optionally rebuild samples list from current Samples~ contents
if ($RebuildSamples) {
  $entries = @()
  if (Test-Path -LiteralPath $Dst_Samples) {
    Get-ChildItem -LiteralPath $Dst_Samples -Directory | ForEach-Object {
      $entries += [pscustomobject]@{
        displayName = $_.Name
        description = ""
        path        = ("Samples~/" + $_.Name)
      }
    }
  }
  $pkg.samples = $entries
}

$pkg | ConvertTo-Json -Depth 20 | Out-File -FilePath $PkgJsonPath -Encoding UTF8
Write-TextMeta $PkgJsonPath

# -------------------- Commit & push --------------------

Push-Location $ProjectRoot

# Stage only the package path
git add -- "Packages/$PackageName"

if (-not (git diff --cached --quiet)) {
  git commit -m "Release $Version" | Out-Null
} else {
  Write-Host "No changes to commit under Packages/$PackageName."
}

if ($ForceBranch) {
  git push -u origin $BranchName --force-with-lease | Out-Null
} else {
  git push -u origin $BranchName | Out-Null
}

if ($TagRelease) {
  git tag -f -a "v$Version" -m "Release $Version" | Out-Null
  git push --force origin tag "v$Version" | Out-Null
}

Pop-Location

# Print install URLs
Write-Host ""
Write-Host "Install via branch:"
Write-Host "  https://github.com/GYOUDNova/Nova.git?path=/Packages/$PackageName#$BranchName"
if ($TagRelease) {
  Write-Host "Install via tag:"
  Write-Host "  https://github.com/GYOUDNova/Nova.git?path=/Packages/$PackageName#v$Version"
}