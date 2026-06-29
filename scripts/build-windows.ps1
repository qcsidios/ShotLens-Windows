param(
    [string]$Version = "",
    [string]$Runtime = "win-x64",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (Get-Content (Join-Path $root "VERSION") -Raw).Trim()
}

if ($Version -notmatch '^v\d+\.\d+\.\d+(?:-beta\.[1-9]\d*)?$') {
    throw "Version must use v1.2.3 or v1.2.3-beta.1 format, got: $Version"
}

$buildDir = Join-Path $root "build\windows"
$publishDir = Join-Path $buildDir "publish"
$installerPath = Join-Path $buildDir "ShotLens-Windows-$Version-Setup.exe"

Remove-Item $buildDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $publishDir -ItemType Directory -Force | Out-Null

if (-not $SkipTests) {
    dotnet run --project (Join-Path $root "tests\ShotLens.Windows.Core.Tests\ShotLens.Windows.Core.Tests.csproj") --configuration Release
}

dotnet publish (Join-Path $root "src\ShotLens.Windows.App\ShotLens.Windows.App.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

Copy-Item (Join-Path $root "src\ShotLens.Windows.App\Resources\ShotLens.ico") $publishDir -Force

$appExe = Join-Path $publishDir "ShotLens.Windows.App.exe"
$smoke = Start-Process $appExe -ArgumentList "--smoke" -Wait -PassThru
if ($smoke.ExitCode -ne 0) {
    throw "Windows app smoke launch failed with exit code $($smoke.ExitCode)."
}

$windowSmoke = Start-Process $appExe -ArgumentList "--smoke-window" -Wait -PassThru
if ($windowSmoke.ExitCode -ne 0) {
    throw "Windows app window smoke launch failed with exit code $($windowSmoke.ExitCode)."
}

$iscc = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($null -eq $iscc) {
    $defaultIscc = Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
    if (Test-Path $defaultIscc) {
        $iscc = Get-Item $defaultIscc
    }
}

if ($null -eq $iscc) {
    throw "Inno Setup compiler ISCC.exe was not found. Install Inno Setup 6 before building the Windows installer."
}

$env:SHOTLENS_VERSION = $Version
$env:SHOTLENS_PUBLISH_DIR = $publishDir
$env:SHOTLENS_INSTALLER_DIR = $buildDir
& $iscc.Source (Join-Path $root "installer\ShotLens.iss")
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $installerPath)) {
    throw "Expected installer was not created: $installerPath"
}

Write-Output $installerPath
