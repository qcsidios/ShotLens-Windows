# OCR 候选 A：ONNX + PP-OCRv5 Mobile

> 状态：阶段 0 候选，尚未选定为正式架构。

## 一、实现边界

候选 A 使用 `RapidOcrNet 2.0.0` 作为 PP-OCR ONNX 推理与后处理薄层，
由 ShotLens 自己负责：

- 模型下载、固定版本和 SHA-256 校验；
- RGB/BGRA 统一为 BGRA；
- 小图等比放大、大图等比缩小；
- 保存缩放比例并把文本框映射回原始物理像素；
- 坐标限制在原图范围；
- 映射 Worker 协议所需的置信度、语言、字号、亮度和顺序；
- 输出原始文本块，不做布局合并。

预处理将短边提升到最多 736 像素，并将长边限制为最多 2000 像素。
该范围避免把极端尺寸直接交给上游内部缩放，坐标统一由 ShotLens 恢复。

## 二、锁定依赖与许可

| 内容 | 锁定版本 | 许可 | 一手来源 |
| --- | --- | --- | --- |
| RapidOcrNet | 2.0.0 | Apache-2.0 | <https://github.com/BobLd/RapidOcrNet/tree/2.0.0> |
| ONNX Runtime | 1.24.3 | MIT | <https://github.com/microsoft/onnxruntime> |
| SkiaSharp | 3.119.1 | MIT | <https://github.com/mono/SkiaSharp> |
| RapidOCR 模型清单 | v3.9.0 | Apache-2.0 | <https://github.com/RapidAI/RapidOCR/blob/v3.9.0/python/rapidocr/default_models.yaml> |
| PaddleOCR 模型 | PP-OCRv5 Mobile | Apache-2.0 | <https://github.com/PaddlePaddle/PaddleOCR> |

`RapidOcrNet 2.0.0` 的 NuGet 元数据将 ONNX Runtime 固定为 1.24.3。
本候选保持该已验证组合，不擅自覆盖为更新但未经适配层验证的版本。

## 三、模型文件

模型不提交到 Git。运行以下命令下载，脚本只在 SHA-256 匹配后把
`.partial` 原子改名为正式文件：

```powershell
.\scripts\download-onnx-ocr-models.ps1
```

| 文件 | SHA-256 |
| --- | --- |
| `ch_PP-OCRv5_mobile_det.onnx` | `4d97c44a20d30a81aad087d6a396b08f786c4635742afc391f6621f5c6ae78ae` |
| `ch_ppocr_mobile_v2.0_cls_mobile.onnx` | `e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c` |
| `ch_PP-OCRv5_rec_mobile.onnx` | `5825fc7ebf84ae7a412be049820b4d86d77620f204a041697b0494669b1742c5` |
| `ppocrv5_dict.txt` | `d1979e9f794c464c0d2e0b70a7fe14dd978e9dc644c0e71f14158cdf8342af1b` |

识别模型与字典必须成对使用。RapidOcrNet 包内默认是拉丁模型，
本项目不会用默认拉丁模型冒充中英双语模型。

## 四、基准方法

两条候选路线必须读取同一份 60 张 Windows/WPF 生成数据集。
运行前逐图核对 Manifest 中的 SHA-256；任何一张不一致都终止。

```powershell
dotnet run `
  --project tools\ShotLens.Windows.Ocr.Benchmark `
  --configuration Release `
  -- run-onnx `
  build\ocr-benchmark\dataset `
  src\ShotLens.Windows.Ocr.Worker `
  build\ocr-benchmark\onnx-report `
  build\ocr-benchmark\onnx-publish
```

报告包含总指标、英文/中文/中英混合分项、冷启动、P50/P95、
峰值内存、Worker 发布体积、模型体积和逐样本识别结果。

## 五、当前证据

2026-06-29 已在 GitHub Actions `windows-latest` 上完成 Windows x64
正式基准，Run ID：`28370343526`。

- 数据集 SHA-256：`dc9899044cbb30a0cce8d817cedc9fea512fed7bdb7ea0977f7a43eccd799b72`
- 模型总大小：21.1 MiB
- Worker `win-x64` framework-dependent 发布体积：71.3 MiB
- 峰值内存：1187.3 MiB
- 启动耗时：296.8 ms
- OCR P50：531.5 ms
- OCR P95：956.0 ms
- 总字符准确率：95.29%
- 英文字符准确率：95.25%，未达到 97% 门槛
- 中文字符准确率：93.52%，未达到 95% 门槛
- 行召回率：96.30%
- 整段漏识别：`en-018`、`zh-016`
- 已确认候选 A 暂未达到选型门槛
- 完整报告在 CI Artifact：`ShotLens-OCR-ONNX-Report`

在候选 B 使用同一数据集并取得同机报告前，不作最终选择。
