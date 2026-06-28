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

if ($Version -notmatch '^v\d+\.\d+\.\d+$') {
    throw "Version must use three-part semver like v1.1.0, got: $Version"
}

$buildDir = Join-Path $root "build\windows"
$publishDir = Join-Path $buildDir "publish"
$packageDir = Join-Path $buildDir "package\ShotLens"
$zipPath = Join-Path $buildDir "ShotLens-Windows-$Version.zip"

Remove-Item $buildDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $publishDir -ItemType Directory -Force | Out-Null
New-Item $packageDir -ItemType Directory -Force | Out-Null

if (-not $SkipTests) {
    dotnet run --project (Join-Path $root "ShotLens.Windows\tests\ShotLens.Windows.Core.Tests\ShotLens.Windows.Core.Tests.csproj") --configuration Release
}

dotnet publish (Join-Path $root "ShotLens.Windows\src\ShotLens.Windows.App\ShotLens.Windows.App.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

Copy-Item (Join-Path $publishDir "*") $packageDir -Recurse
Copy-Item (Join-Path $root "README.md") $packageDir
Copy-Item (Join-Path $root "LICENSE") $packageDir

Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -Force
Write-Output $zipPath
