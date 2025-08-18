<# 
.SYNOPSIS
Build a UPM package from a Unity project and publish it to an orphan "package-only" branch
so that the repository root IS the package (no ?path= needed).

.EXAMPLE
./pack.ps1 -Version 1.1.0 -TagRelease
# -> pushes branch upm/v1.1.0 and tag v1.1.0
# Install URL: https://github.com/<org>/<repo>.git#upm/v1.1.0  (or #v1.1.0)

.NOTES
- The script copies from Assets/* into a staging package tree, preserving GUIDs when possible.
- Uses a temporary git worktree to create an orphan branch that contains ONLY the package.
- The working tree of your main repo must be clean.
#>

param(
  [Parameter(Mandatory = $true)][string]$Version,

  # Git options
  [string]$BaseBranch = "main",
  [string]$BranchName = "",           # default: upm/v<Version>
  [switch]$ForceBranch,               # allow overwriting existing branch
  [switch]$TagRelease,                # also create/push tag v<Version>

  # package.json defaults (used when creating a new one or when -OverrideCoreMetadata is set)
  [string]$PackageName    = "com.gyoudnova.handrecognition",
  [string]$DisplayName    = "Hand Recognition",
  [string]$Description    = "This is the package for hand recognition project created by Gyoud Nova",
  [string]$UnityVersion   = "6000.0",
  [string]$UnityRelease   = "39f1",
  [string]$AuthorName     = "Gyoud Nova",

  [switch]$OverrideCoreMetadata,      # overwrite name/display/unity/description in package.json
  [switch]$RebuildSamples             # rebuild samples[] from Samples~ subfolders
)

# -------------------- Setup --------------------
$ErrorActionPreference = "Stop"

# Accept "v1.2.3" or "1.2.3"
if ($Version -match '^[vV](.+)$') { $Version = $Matches[1] }

# Branch name default
if ([string]::IsNullOrWhiteSpace($BranchName)) { $BranchName = "upm/v$Version" }

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

# Require clean working tree
Push-Location $ProjectRoot
if (git status --porcelain) {
  Pop-Location
  throw "Working tree is not clean. Stash/commit your changes first."
}

# Fetch & sanity-check remote branch
git fetch origin | Out-Null
$remoteExists = -not [string]::IsNullOrWhiteSpace((git ls-remote --heads origin $BranchName))

if ($remoteExists -and -not $ForceBranch) {
  Pop-Location
  throw "Remote branch '$BranchName' already exists. Re-run with -ForceBranch to replace it."
}
Pop-Location

# -------------------- Paths --------------------
$Assets               = Join-Path $ProjectRoot "Assets"
$Src_Scripts          = Join-Path $Assets "Scripts"
$Src_Images           = Join-Path $Assets "Images"
$Src_Prefabs          = Join-Path $Assets "Prefabs"
$Src_Samples          = Join-Path $Assets "Samples"
$Src_Streaming        = Join-Path $Assets "StreamingAssets"
$Src_Tests            = Join-Path $Assets "Tests"
$Src_UIToolkit        = Join-Path $Assets "UI Toolkit"

# Staging dir (package root at /)
$StageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("upm-stage-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $StageRoot | Out-Null

# Package-relative destinations (under StageRoot)
$Dst_Editor           = Join-Path $StageRoot "Editor"
$Dst_Images           = Join-Path $StageRoot "Images"
$Dst_Runtime          = Join-Path $StageRoot "Runtime"
$Dst_Runtime_Prefabs  = Join-Path $Dst_Runtime "Prefabs"
$Dst_Samples          = Join-Path $StageRoot "Samples~"       # hidden until import
$Dst_Streaming        = Join-Path $StageRoot "StreamingAssets"
$Dst_Tests            = Join-Path $StageRoot "Tests"
$Dst_UIToolkit        = Join-Path $StageRoot "UI Toolkit"

# -------------------- Helpers --------------------
function New-Dir([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
  }
}
function New-GuidHex { ([guid]::NewGuid().ToString("N")) }

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

function Copy-DirContents {
  param(
    [Parameter(Mandatory = $true)][string]$From,
    [Parameter(Mandatory = $true)][string]$To,
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

function Copy-TreeTo {
  param(
    [Parameter(Mandatory = $true)][string]$From,
    [Parameter(Mandatory = $true)][string]$To
  )
  if (-not (Test-Path -LiteralPath $From -PathType Container)) { return }
  New-Dir $To
  Get-ChildItem -LiteralPath $From -Force | ForEach-Object {
    $dest = Join-Path $To $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force -ErrorAction Stop
  }
}

# -------------------- Build package tree in $StageRoot --------------------

# Editor <- Assets/Scripts  (excluding "Dev Utilities")
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

# Samples~ <- Assets/Samples   (no Samples~.meta on purpose)
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

# Copy top-level docs if present
$ReadmePath  = Join-Path $ProjectRoot "README.md"
$LicensePath = Join-Path $ProjectRoot "LICENSE"
if (Test-Path $ReadmePath)  { Copy-Item $ReadmePath  (Join-Path $StageRoot "README.md")  -Force }
if (Test-Path $LicensePath) { Copy-Item $LicensePath (Join-Path $StageRoot "LICENSE")     -Force }

foreach ($p in @("README.md","LICENSE")) {
  $full = Join-Path $StageRoot $p
  if (Test-Path -LiteralPath $full) { Write-TextMeta $full }
}

# .gitattributes at package root
$PkgGitAttr = Join-Path $StageRoot ".gitattributes"
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
$PkgJsonPath = Join-Path $StageRoot "package.json"
# Create new or use existing from stage dir if you copy one before
if (Test-Path -LiteralPath $PkgJsonPath) {
  $pkg = Get-Content -LiteralPath $PkgJsonPath -Raw | ConvertFrom-Json
} else {
  $pkg = [pscustomobject]@{
    name         = $PackageName
    version      = $Version
    displayName  = $DisplayName
    description  = $Description
    unity        = $UnityVersion
    unityRelease = $UnityRelease
    keywords     = @("Webcam","Hand Gesture","Sign language")
    author       = @{ name = $AuthorName }
    dependencies = @{
      "com.gilzoide.sqlite-net"      = "1.2.3"
      "com.github.homuler.mediapipe" = "0.16.1"
      "com.unity.ugui"               = "2.0.0"
      "com.unity.editorcoroutines"   = "1.0.0"
    }
    samples      = @(
      @{ displayName="Sample Menu UI";   description="Contains a sample scene and scripts for a menu UI"; path="Samples~/SampleMenu" },
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

# -------------------- Publish to orphan branch at repo root --------------------
Push-Location $ProjectRoot

# Create a temporary worktree, then switch it to an ORPHAN branch
$WorktreeDir = Join-Path ([System.IO.Path]::GetTempPath()) ("upm-wt-" + [guid]::NewGuid().ToString("N"))
git worktree add -f $WorktreeDir $BaseBranch | Out-Null

Push-Location $WorktreeDir
git switch --orphan $BranchName | Out-Null

# Remove everything except .git to ensure branch root is empty
Get-ChildItem -Force | Where-Object { $_.Name -ne ".git" } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Copy staged package (root -> repo root)
Copy-Item -Recurse -Force (Join-Path $StageRoot "*") .

# Ensure .meta for root text files
foreach ($p in @(".gitattributes","README.md","LICENSE","package.json")) {
  if (Test-Path -LiteralPath $p) { Write-TextMeta $p }
}

git add -A
git commit -m "UPM package v$Version (package-only branch)" | Out-Null

if ($ForceBranch) {
  git push -u origin $BranchName --force-with-lease | Out-Null
} else {
  git push -u origin $BranchName | Out-Null
}

if ($TagRelease) {
  git tag -f -a "v$Version" -m "UPM package v$Version" | Out-Null
  git push --force origin tag "v$Version" | Out-Null
}

Pop-Location  # worktree
# Remove the temporary worktree and staging dir
git worktree remove $WorktreeDir --force | Out-Null
Pop-Location  # project
Remove-Item -Recurse -Force $StageRoot -ErrorAction SilentlyContinue

# -------------------- Print install URLs --------------------
Write-Host ""
Write-Host "Install via branch:"
Write-Host "  https://github.com/GYOUDNova/Nova.git#$BranchName"
if ($TagRelease) {
  Write-Host "Install via tag:"
  Write-Host "  https://github.com/GYOUDNova/Nova.git#v$Version"
}
