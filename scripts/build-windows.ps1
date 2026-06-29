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
$channel = if ($Version -match '-beta\.') { "beta" } else { "stable" }
$installerPrefix = if ($channel -eq "beta") { "ShotLens-Beta" } else { "ShotLens-Windows" }
$installerPath = Join-Path $buildDir "$installerPrefix-$Version-Setup.exe"

Remove-Item $buildDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $publishDir -ItemType Directory -Force | Out-Null

if (-not $SkipTests) {
    dotnet restore (Join-Path $root "ShotLens.Windows.sln") --locked-mode
    if ($LASTEXITCODE -ne 0) {
        throw "Locked dependency restore failed with exit code $LASTEXITCODE."
    }

    dotnet test (Join-Path $root "ShotLens.Windows.sln") `
        --configuration Release `
        --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE."
    }
}

dotnet publish (Join-Path $root "src\ShotLens.Windows.App\ShotLens.Windows.App.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true
if ($LASTEXITCODE -ne 0) {
    throw "Windows publish failed with exit code $LASTEXITCODE."
}

dotnet publish (Join-Path $root "src\ShotLens.Windows.Ocr.Worker\ShotLens.Windows.Ocr.Worker.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "Windows OCR Worker publish failed with exit code $LASTEXITCODE."
}

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
$env:SHOTLENS_CHANNEL = $channel
$env:SHOTLENS_PUBLISH_DIR = $publishDir
$env:SHOTLENS_INSTALLER_DIR = $buildDir
& $iscc.Source (Join-Path $root "installer\ShotLens.iss")
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $installerPath)) {
    throw "Expected installer was not created: $installerPath"
}

$installerInfo = Get-Item $installerPath
if ($installerInfo.Length -le 0) {
    throw "Installer is empty: $installerPath"
}

$hash = (Get-FileHash $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output "Installer: $installerPath"
Write-Output "SHA256: $hash"
