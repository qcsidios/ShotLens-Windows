param(
    [Parameter(Mandatory = $true)]
    [string]$StableInstaller,
    [Parameter(Mandatory = $true)]
    [string]$Beta1Installer,
    [Parameter(Mandatory = $true)]
    [string]$Beta2Installer,
    [string]$ExpectedBeta2Version = "v0.2.0-beta.2"
)

$ErrorActionPreference = "Stop"
$uninstallRoots = @(
    "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
    "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
    "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
)

function Install-ShotLens([string]$Path) {
    if (-not (Test-Path $Path)) {
        throw "Installer not found: $Path"
    }

    $process = Start-Process $Path `
        -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS" `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Installer failed with exit code $($process.ExitCode): $Path"
    }
}

function Get-ShotLensInstall([string]$DisplayName) {
    $entries = foreach ($root in $uninstallRoots) {
        if (Test-Path $root) {
            Get-ChildItem $root | ForEach-Object {
                Get-ItemProperty $_.PSPath -ErrorAction SilentlyContinue
            }
        }
    }

    $matches = @($entries | Where-Object { $_.DisplayName -eq $DisplayName })
    if ($matches.Count -ne 1) {
        throw "Expected one '$DisplayName' uninstall entry, found $($matches.Count)."
    }

    return $matches[0]
}

Install-ShotLens $StableInstaller
Install-ShotLens $Beta1Installer

$stable = Get-ShotLensInstall "ShotLens"
$beta = Get-ShotLensInstall "ShotLens Beta"
if ($stable.InstallLocation -eq $beta.InstallLocation) {
    throw "Stable and beta installations use the same directory."
}

$stableSettings = Join-Path $env:APPDATA "ShotLens\settings.json"
$betaSettings = Join-Path $env:APPDATA "ShotLens Beta\stage0-upgrade-sentinel.json"
New-Item (Split-Path $stableSettings) -ItemType Directory -Force | Out-Null
New-Item (Split-Path $betaSettings) -ItemType Directory -Force | Out-Null
if (-not (Test-Path $stableSettings)) {
    Set-Content $stableSettings '{"stable":true}' -Encoding utf8NoBOM
}
Set-Content $betaSettings '{"sentinel":"keep"}' -Encoding utf8NoBOM
$stableHash = (Get-FileHash $stableSettings -Algorithm SHA256).Hash
$betaHash = (Get-FileHash $betaSettings -Algorithm SHA256).Hash

Install-ShotLens $Beta2Installer

$stableAfter = Get-ShotLensInstall "ShotLens"
$betaAfter = Get-ShotLensInstall "ShotLens Beta"
if ($stableAfter.InstallLocation -ne $stable.InstallLocation) {
    throw "Stable installation location changed during beta upgrade."
}
if ($betaAfter.InstallLocation -ne $beta.InstallLocation) {
    throw "Beta installation location changed during beta upgrade."
}
if ((Get-FileHash $stableSettings -Algorithm SHA256).Hash -ne $stableHash) {
    throw "Stable settings changed during beta upgrade."
}
if ((Get-FileHash $betaSettings -Algorithm SHA256).Hash -ne $betaHash) {
    throw "Beta settings sentinel changed during beta upgrade."
}

$betaVersion = (Get-Content (Join-Path $betaAfter.InstallLocation "VERSION") -Raw).Trim()
if ($betaVersion -ne $ExpectedBeta2Version) {
    throw "Expected beta version $ExpectedBeta2Version, got $betaVersion."
}

$betaExe = Join-Path $betaAfter.InstallLocation "ShotLens.Windows.App.exe"
$smoke = Start-Process $betaExe -ArgumentList "--smoke" -Wait -PassThru
if ($smoke.ExitCode -ne 0) {
    throw "Upgraded beta smoke check failed with exit code $($smoke.ExitCode)."
}

Write-Output "Stable and beta installations are isolated."
Write-Output "Beta upgrade preserved settings and installed $ExpectedBeta2Version."
