using System.Diagnostics;
using RapidOcrNet;
using ShotLens.Windows.Core.Ocr;
using SkiaSharp;

namespace ShotLens.Windows.Ocr.Worker.Onnx;

public sealed class OnnxPaddleOcrEngine(string baseDirectory) : IDisposable
{
    public const string Version =
        "RapidOcrNet-2.0.0+OnnxRuntime-1.24.3";

    private RapidOcr? _ocr;

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

        var modelDirectory = OnnxOcrModelCatalog.ResolveDirectory(
            baseDirectory);
        var ocr = EnsureInitialized(modelDirectory);
        using var prepared = OnnxOcrImagePreprocessor.Prepare(
            source,
            minimumShortSide: 736,
            maximumLongSide: 2000);
        var stopwatch = Stopwatch.StartNew();
        var result = ocr.Detect(
            prepared.Bitmap,
            RapidOcrOptions.PythonCompat);
        stopwatch.Stop();
        return new OcrProtocolResponse(
            OcrProtocol.CurrentVersion,
            request.RequestId,
            OcrEngineIds.OnnxPaddleOcr,
            Version,
            stopwatch.ElapsedMilliseconds,
            OnnxOcrResultMapper.Map(
                result.TextBlocks,
                source,
                prepared.Scale));
    }

    public void Dispose() => _ocr?.Dispose();

    private RapidOcr EnsureInitialized(string modelDirectory)
    {
        if (_ocr is not null)
        {
            return _ocr;
        }

        var stopwatch = Stopwatch.StartNew();
        OnnxOcrModelVerifier.VerifyAll(modelDirectory);
        var ocr = new RapidOcr();
        try
        {
            ocr.InitModels(
                ModelPath(modelDirectory, "ch_PP-OCRv5_mobile_det.onnx"),
                ModelPath(
                    modelDirectory,
                    "ch_ppocr_mobile_v2.0_cls_mobile.onnx"),
                ModelPath(modelDirectory, "ch_PP-OCRv5_rec_mobile.onnx"),
                ModelPath(modelDirectory, "ppocrv5_dict.txt"));
        }
        catch
        {
            ocr.Dispose();
            throw;
        }

        stopwatch.Stop();
        StartupMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        _ocr = ocr;
        return _ocr;
    }

    private static string ModelPath(
        string modelDirectory,
        string fileName) =>
        Path.Combine(modelDirectory, fileName);
}
