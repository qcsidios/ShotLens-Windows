# OCR 候选 B：PaddleSharp

> 状态：阶段 0 候选，尚未选定为正式架构。

## 一、实现边界

候选 B 使用 Sdcb 的 PaddleSharp 绑定、PaddleOCR V5 本地模型和
Windows x64 MKL 运行时。ShotLens 只写适配层：

- 使用 `LocalFullModels.ChineseV5`；
- CPU 使用 MKL，缓存容量固定为 1；
- 截图文字默认水平，关闭旋转检测和 180° 分类；
- Detector 不主动缩小长边，避免小字在检测前丢失；
- 将 `RotatedRect` 向外取整并限制在原始物理像素范围；
- 输出原始文本区域，不做布局合并；
- 继续使用与候选 A 完全相同的数据集、评分器和报告器。

## 二、锁定依赖与许可

| 内容 | 锁定版本 | 许可 | 一手来源 |
| --- | --- | --- | --- |
| Sdcb.PaddleOCR | 3.3.1 | Apache-2.0 | <https://github.com/sdcb/PaddleSharp> |
| Sdcb.PaddleOCR.Models.Local | 3.3.1 | Apache-2.0 | <https://www.nuget.org/packages/Sdcb.PaddleOCR.Models.Local/3.3.1> |
| Sdcb.PaddleOCR.Models.LocalV5 | 3.0.0（由 Local 3.3.1 锁定） | Apache-2.0 | <https://www.nuget.org/packages/Sdcb.PaddleOCR.Models.LocalV5/3.0.0> |
| Sdcb.PaddleInference.runtime.win64.mkl | 3.3.1.70 | Apache-2.0 | <https://www.nuget.org/packages/Sdcb.PaddleInference.runtime.win64.mkl/3.3.1.70> |
| OpenCvSharp4.runtime.win | 4.11.0.20250507 | Apache-2.0 | <https://www.nuget.org/packages/OpenCvSharp4.runtime.win/4.11.0.20250507> |
| PaddleOCR | PP-OCRv5 | Apache-2.0 | <https://github.com/PaddlePaddle/PaddleOCR> |

`Sdcb.PaddleOCR 3.3.1` 目标为 .NET Standard 2.0，可由 .NET 8 使用。
本候选只验证 CPU x64，不引入 CUDA、TensorRT 或 GPU 要求。

## 三、已知体积风险

NuGet 下载体积初查：

- PaddleInference win64 MKL：约 78 MiB；
- OpenCV Windows runtime：约 38 MiB；
- PaddleOCR LocalV5 模型包：约 95 MiB；
- 双候选合并发布目录：约 473 MiB。

报告按文件归属分别计算候选有效发布体积，公共 Worker、协议和 Skia
文件两边都计入；Paddle/OpenCV/MKL 文件只计入候选 B。

## 四、基准命令

```powershell
dotnet run `
  --project tools\ShotLens.Windows.Ocr.Benchmark `
  --configuration Release `
  -- run-paddlesharp `
  build\ocr-benchmark\dataset `
  src\ShotLens.Windows.Ocr.Worker `
  build\ocr-benchmark\paddlesharp-report `
  build\ocr-benchmark\candidates-publish
```

Windows CI 必须运行真实模型测试和完整 60 张基准。取得同机报告前，
不得选择或拒绝候选 B。
