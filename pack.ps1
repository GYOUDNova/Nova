param(
  [Parameter(Mandatory=$true)][string]$Version,
  [Parameter(Mandatory=$true)][string]$PackageRepoPath,
  [string]$BaseBranch = "main",
  [string]$BranchName = "",
  [switch]$ForceBranch,
  [switch]$TagRelease,
  [string]$PackageName    = "com.gyoudnova.handrecognition",
  [string]$DisplayName    = "Hand Recognition",
  [string]$Description    = "This is the package for hand recognition project created by Gyoud Nova",
  [string]$UnityVersion   = "6000.0",
  [string]$UnityRelease   = "39f1",
  [string]$AuthorName     = "Gyoud Nova",
  [switch]$OverrideCoreMetadata,
  [switch]$RebuildSamples
)

$ErrorActionPreference = "Stop"
if ($Version -match '^[vV](.+)$') { $Version = $Matches[1] }
if (-not $BranchName -or [string]::IsNullOrWhiteSpace($BranchName)) { $BranchName = "release-v$Version" }

function Require($name) { if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw "Missing dependency in PATH: $name" } }
Require git

$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = & git -C $ScriptDir rev-parse --show-toplevel 2>$null
if (-not $ProjectRoot) { $ProjectRoot = (Resolve-Path $ScriptDir).Path }

if (-not (Test-Path -LiteralPath $PackageRepoPath)) { throw "PackageRepoPath '$PackageRepoPath' not found." }
$PackageRepoPath = (Resolve-Path -LiteralPath $PackageRepoPath).Path

$projRootN = [IO.Path]::GetFullPath($ProjectRoot + [IO.Path]::DirectorySeparatorChar)
$destRootN = [IO.Path]::GetFullPath($PackageRepoPath + [IO.Path]::DirectorySeparatorChar)
if ($destRootN.StartsWith($projRootN, [System.StringComparison]::OrdinalIgnoreCase)) { throw "Package repo '$destRootN' cannot be inside dev repo '$projRootN'." }

Write-Host "==> Packing $PackageName v$Version"
Write-Host "Dev repo:       $ProjectRoot"
Write-Host "Package repo:   $PackageRepoPath"
Write-Host "Base branch:    $BaseBranch"
Write-Host "Package branch: $BranchName"

if (-not (Test-Path (Join-Path $PackageRepoPath ".git"))) { throw "ERROR: $PackageRepoPath is not a git repo." }

Push-Location $PackageRepoPath
git fetch origin | Out-Null
git checkout $BaseBranch | Out-Null
git pull --ff-only | Out-Null
$remoteExists = -not [string]::IsNullOrWhiteSpace((git ls-remote --heads origin $BranchName))
if ($remoteExists -and -not $ForceBranch) { Pop-Location; throw "Remote branch '$BranchName' already exists. Re-run with -ForceBranch to overwrite." }
if ((git branch --list $BranchName)) { git branch -D $BranchName | Out-Null }
git checkout -B $BranchName $BaseBranch | Out-Null
if (git status --porcelain) { Pop-Location; throw "ERROR: package repo has uncommitted changes." }
Get-ChildItem -LiteralPath $PackageRepoPath -Force | Where-Object { $_.Name -ne ".git" } | Remove-Item -Recurse -Force -ErrorAction Stop
Pop-Location

function New-Dir([string]$Path) { if (-not (Test-Path -LiteralPath $Path)) { New-Item -ItemType Directory -Force -Path $Path | Out-Null } }
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
  if (Test-Path -LiteralPath $srcMeta) { Copy-Item -LiteralPath $srcMeta -Destination $dstMeta -Force } else { Write-FolderMeta $DstFolder }
}
function Copy-DirContents {
  param(
    [Parameter(Mandatory=$true)][string]$From,
    [Parameter(Mandatory=$true)][string]$To,
    [string[]]$ExcludeChildNames = @()
  )
  if (-not (Test-Path -LiteralPath $From -PathType Container)) { Write-Host "SKIP missing: $From"; return }
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
  param([Parameter(Mandatory=$true)][string]$From, [Parameter(Mandatory=$true)][string]$To)
  if (-not (Test-Path -LiteralPath $From -PathType Container)) { Write-Host "SKIP missing: $From"; return }
  New-Dir $To
  Get-ChildItem -LiteralPath $From -Force | ForEach-Object {
    $dest = Join-Path $To $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force -ErrorAction Stop
  }
}

$Assets               = Join-Path $ProjectRoot "Assets"
$Src_Scripts          = Join-Path $Assets "Scripts"
$Src_Images           = Join-Path $Assets "Images"
$Src_Prefabs          = Join-Path $Assets "Prefabs"
$Src_Samples          = Join-Path $Assets "Samples"
$Src_Streaming        = Join-Path $Assets "StreamingAssets"
$Src_Tests            = Join-Path $Assets "Tests"
$Src_UIToolkit        = Join-Path $Assets "UI Toolkit"

$Dst_Editor           = Join-Path $PackageRepoPath "Editor"
$Dst_Images           = Join-Path $PackageRepoPath "Images"
$Dst_Runtime          = Join-Path $PackageRepoPath "Runtime"
$Dst_Runtime_Prefabs  = Join-Path $Dst_Runtime "Prefabs"
$Dst_Samples          = Join-Path $PackageRepoPath "Samples~"
$Dst_Streaming        = Join-Path $PackageRepoPath "StreamingAssets"
$Dst_Tests            = Join-Path $PackageRepoPath "Tests"
$Dst_UIToolkit        = Join-Path $PackageRepoPath "UI Toolkit"

Copy-DirContents -From $Src_Scripts -To $Dst_Editor -ExcludeChildNames @("Dev Utilities")
Copy-Or-Make-FolderMeta -SrcFolder $Src_Scripts -DstFolder $Dst_Editor
Copy-TreeTo -From $Src_Images -To $Dst_Images
Copy-Or-Make-FolderMeta -SrcFolder $Src_Images -DstFolder $Dst_Images
New-Dir $Dst_Runtime
Write-FolderMeta $Dst_Runtime
if (Test-Path -LiteralPath $Src_Prefabs) {
  Copy-TreeTo -From $Src_Prefabs -To $Dst_Runtime_Prefabs
  Copy-Or-Make-FolderMeta -SrcFolder $Src_Prefabs -DstFolder $Dst_Runtime_Prefabs
}
Copy-TreeTo -From $Src_Samples -To $Dst_Samples
Copy-TreeTo -From $Src_Streaming -To $Dst_Streaming
Copy-Or-Make-FolderMeta -SrcFolder $Src_Streaming -DstFolder $Dst_Streaming
Copy-TreeTo -From $Src_Tests -To $Dst_Tests
Copy-Or-Make-FolderMeta -SrcFolder $Src_Tests -DstFolder $Dst_Tests
Copy-TreeTo -From $Src_UIToolkit -To $Dst_UIToolkit
Copy-Or-Make-FolderMeta -SrcFolder $Src_UIToolkit -DstFolder $Dst_UIToolkit

$DevUtilMeta = Join-Path $Dst_Editor "Dev Utilities.meta"
Remove-Item -LiteralPath $DevUtilMeta -Force -ErrorAction SilentlyContinue

$ReadmePath     = Join-Path $PackageRepoPath "README.md"
$LicensePath    = Join-Path $PackageRepoPath "LICENSE"
$GitIgnoreSrc   = Join-Path $ProjectRoot ".gitignore"
$GitAttributes  = Join-Path $ProjectRoot ".gitattributes"

if (Test-Path (Join-Path $ProjectRoot "README.md"))     { Copy-Item (Join-Path $ProjectRoot "README.md")     $ReadmePath     -Force }
if (Test-Path (Join-Path $ProjectRoot "LICENSE"))    { Copy-Item (Join-Path $ProjectRoot "LICENSE")    $LicensePath    -Force }
if (Test-Path -LiteralPath $GitIgnoreSrc)  { Copy-Item -LiteralPath $GitIgnoreSrc  -Destination (Join-Path $PackageRepoPath ".gitignore")   -Force }
if (Test-Path -LiteralPath $GitAttributes) { Copy-Item -LiteralPath $GitAttributes -Destination (Join-Path $PackageRepoPath ".gitattributes") -Force }

foreach ($p in @($ReadmePath,$LicensePath)) { if (Test-Path -LiteralPath $p) { Write-TextMeta $p } }

$PkgJsonPath = Join-Path $PackageRepoPath "package.json"
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
$pkg.version = $Version
if ($OverrideCoreMetadata -or -not (Test-Path -LiteralPath $PkgJsonPath)) {
  $pkg.name         = $PackageName
  $pkg.displayName  = $DisplayName
  $pkg.description  = $Description
  $pkg.unity        = $UnityVersion
  $pkg.unityRelease = $UnityRelease
  if (-not $pkg.author) { $pkg | Add-Member -NotePropertyName author -NotePropertyValue @{ name = $AuthorName } }
  elseif (-not $pkg.author.name) { $pkg.author.name = $AuthorName }
}
if ($RebuildSamples) {
  $samplesRoot = Join-Path $PackageRepoPath "Samples~"
  $entries = @()
  if (Test-Path -LiteralPath $samplesRoot) {
    Get-ChildItem -LiteralPath $samplesRoot -Directory | ForEach-Object {
      $entries += [pscustomobject]@{ displayName=$_.Name; description=""; path=("Samples~/" + $_.Name) }
    }
  }
  $pkg.samples = $entries
}
$pkg | ConvertTo-Json -Depth 20 | Out-File -FilePath $PkgJsonPath -Encoding UTF8
Write-TextMeta $PkgJsonPath

Push-Location $PackageRepoPath
git add -A
if (-not (git diff --cached --quiet)) { git commit -m "Release $Version" | Out-Null } else { Write-Host "No changes to commit." }
if ($ForceBranch) { git push -u origin $BranchName --force-with-lease | Out-Null } else { git push -u origin $BranchName | Out-Null }
if ($TagRelease) { git tag -f -a "v$Version" -m "Release $Version" | Out-Null; git push --force origin tag "v$Version" | Out-Null }
Pop-Location

Write-Host "`n✅ Done."
Write-Host "Test install via branch:"
Write-Host "  https://github.com/GYOUDNova/Nova_Package.git#$BranchName"
if ($TagRelease) {
  Write-Host "Or via tag:"
  Write-Host "  https://github.com/GYOUDNova/Nova_Package.git#v$Version"
}