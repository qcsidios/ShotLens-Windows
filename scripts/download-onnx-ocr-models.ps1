param(
    [string]$OutputDirectory = (
        Join-Path $PSScriptRoot `
            "..\src\ShotLens.Windows.Ocr.Worker\models\onnx-paddleocr")
)

$ErrorActionPreference = "Stop"

$models = @(
    @{
        Name = "ch_PP-OCRv5_mobile_det.onnx"
        Uri = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/onnx/PP-OCRv5/det/ch_PP-OCRv5_det_mobile.onnx"
        Sha256 = "4d97c44a20d30a81aad087d6a396b08f786c4635742afc391f6621f5c6ae78ae"
    },
    @{
        Name = "ch_ppocr_mobile_v2.0_cls_mobile.onnx"
        Uri = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/onnx/PP-OCRv4/cls/ch_ppocr_mobile_v2.0_cls_mobile.onnx"
        Sha256 = "e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c"
    },
    @{
        Name = "ch_PP-OCRv5_rec_mobile.onnx"
        Uri = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/onnx/PP-OCRv5/rec/ch_PP-OCRv5_rec_mobile.onnx"
        Sha256 = "5825fc7ebf84ae7a412be049820b4d86d77620f204a041697b0494669b1742c5"
    },
    @{
        Name = "ppocrv5_dict.txt"
        Uri = "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/paddle/PP-OCRv5/rec/ch_PP-OCRv5_rec_mobile/ppocrv5_dict.txt"
        Sha256 = "d1979e9f794c464c0d2e0b70a7fe14dd978e9dc644c0e71f14158cdf8342af1b"
    }
)

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

foreach ($model in $models) {
    $destination = Join-Path $OutputDirectory $model.Name
    $partial = "$destination.partial"
    if (Test-Path $destination) {
        $existingHash = (
            Get-FileHash $destination -Algorithm SHA256
        ).Hash.ToLowerInvariant()
        if ($existingHash -eq $model.Sha256) {
            Write-Host "模型已存在并通过校验：$($model.Name)"
            continue
        }

        Remove-Item $destination -Force
    }

    Remove-Item $partial -Force -ErrorAction SilentlyContinue
    Invoke-WebRequest $model.Uri -OutFile $partial
    $downloadHash = (
        Get-FileHash $partial -Algorithm SHA256
    ).Hash.ToLowerInvariant()
    if ($downloadHash -ne $model.Sha256) {
        Remove-Item $partial -Force
        throw "OCR 模型 SHA-256 不匹配：$($model.Name)"
    }

    Move-Item $partial $destination -Force
    Write-Host "模型下载并校验完成：$($model.Name)"
}
