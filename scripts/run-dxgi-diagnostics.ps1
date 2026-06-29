param(
    [string]$ExecutablePath = "",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $ExecutablePath = Join-Path $env:LOCALAPPDATA `
        "Programs\ShotLens Beta\ShotLens.Windows.App.exe"
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputDirectory = Join-Path ([Environment]::GetFolderPath("Desktop")) `
        "ShotLens-DXGI-$timestamp"
}

if (-not (Test-Path $ExecutablePath)) {
    throw "未找到 ShotLens 可执行文件：$ExecutablePath"
}

$process = Start-Process `
    -FilePath $ExecutablePath `
    -ArgumentList @("--capture-diagnostics", "`"$OutputDirectory`"") `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    $errorPath = Join-Path $OutputDirectory "error.txt"
    $details = if (Test-Path $errorPath) {
        Get-Content $errorPath -Raw
    } else {
        "未生成错误详情。"
    }
    throw "DXGI 截图诊断失败，退出码 $($process.ExitCode)。$details"
}

$manifestPath = Join-Path $OutputDirectory "manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "诊断未生成 manifest.json。"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
if (@($manifest.outputs).Count -eq 0) {
    throw "诊断未发现任何显示器，请在本机交互式桌面会话中重试。"
}

$failedOutputs = @($manifest.outputs | Where-Object { $_.status -ne "captured" })
if ($failedOutputs.Count -gt 0) {
    throw "有 $($failedOutputs.Count) 个显示器未成功截图，请检查 $manifestPath。"
}

foreach ($output in $manifest.outputs) {
    $pngPath = Join-Path $OutputDirectory $output.fileName
    if (-not (Test-Path $pngPath)) {
        throw "缺少显示器截图：$pngPath"
    }
}

Write-Output "DXGI 截图诊断通过：$OutputDirectory"
Write-Output "显示器数量：$($manifest.outputs.Count)"
