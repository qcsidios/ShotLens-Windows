using System.Diagnostics;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using ShotLens.Windows.Core.Ocr;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Worker.PaddleSharp;

public sealed class PaddleSharpOcrEngine : IDisposable
{
    public const string Version =
        "Sdcb.PaddleOCR-3.3.1+PaddleInference-3.3.1.70";
    public const string ModelVersion =
        "Sdcb.PaddleOCR.Models.Local-3.3.1/LocalV5-3.0.0";

    private PaddleOcrAll? _ocr;

    public double StartupMilliseconds { get; private set; }

    public OcrProtocolResponse Recognize(OcrProtocolRequest request)
    {
        using var source = SKBitmap.Decode(request.ImagePath)
            ?? throw new OcrProtocolException("OCR 图片无法解码。");
        if (source.Width != request.ImageSize.Width
            || source.Height != request.ImageSize.Height)
        {
            throw new OcrProtocolException(
                "OCR 请求尺寸与图片物理像素尺寸不一致。");
        }

        var ocr = EnsureInitialized();
        var stopwatch = Stopwatch.StartNew();
        using var image = Cv2.ImRead(
            request.ImagePath,
            ImreadModes.Color);
        if (image.Empty())
        {
            throw new OcrProtocolException("PaddleSharp 无法解码 OCR 图片。");
        }

        var result = ocr.Run(image);
        stopwatch.Stop();
        return new OcrProtocolResponse(
            OcrProtocol.CurrentVersion,
            request.RequestId,
            OcrEngineIds.PaddleSharp,
            Version,
            stopwatch.ElapsedMilliseconds,
            PaddleSharpOcrResultMapper.Map(result.Regions, source));
    }

    public void Dispose() => _ocr?.Dispose();

    private PaddleOcrAll EnsureInitialized()
    {
        if (_ocr is not null)
        {
            return _ocr;
        }

        var stopwatch = Stopwatch.StartNew();
        var ocr = new PaddleOcrAll(
            LocalFullModels.ChineseV5,
            PaddleDevice.Mkldnn(
                cacheCapacity: 1,
                cpuMathThreadCount: 0,
                memoryOptimized: true,
                glogEnabled: false))
        {
            AllowRotateDetection = false,
            Enable180Classification = false
        };
        ocr.Detector.MaxSize = null;
        stopwatch.Stop();
        StartupMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        _ocr = ocr;
        return _ocr;
    }
}
