using System.Security.Cryptography;

namespace ShotLens.Windows.Ocr.Worker.Onnx;

public static class OnnxOcrModelCatalog
{
    public const string Version = "RapidOCR-v3.9.0/PP-OCRv5";

    public const string RelativeDirectory = "models/onnx-paddleocr";

    public static readonly OnnxOcrModelFile[] Files =
    [
        new(
            "ch_PP-OCRv5_mobile_det.onnx",
            new Uri(
                "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/onnx/PP-OCRv5/det/ch_PP-OCRv5_det_mobile.onnx"),
            "4d97c44a20d30a81aad087d6a396b08f786c4635742afc391f6621f5c6ae78ae"),
        new(
            "ch_ppocr_mobile_v2.0_cls_mobile.onnx",
            new Uri(
                "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/onnx/PP-OCRv4/cls/ch_ppocr_mobile_v2.0_cls_mobile.onnx"),
            "e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c"),
        new(
            "ch_PP-OCRv5_rec_mobile.onnx",
            new Uri(
                "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/onnx/PP-OCRv5/rec/ch_PP-OCRv5_rec_mobile.onnx"),
            "5825fc7ebf84ae7a412be049820b4d86d77620f204a041697b0494669b1742c5"),
        new(
            "ppocrv5_dict.txt",
            new Uri(
                "https://www.modelscope.cn/models/RapidAI/RapidOCR/resolve/v3.9.0/paddle/PP-OCRv5/rec/ch_PP-OCRv5_rec_mobile/ppocrv5_dict.txt"),
            "d1979e9f794c464c0d2e0b70a7fe14dd978e9dc644c0e71f14158cdf8342af1b")
    ];

    public static string ResolveDirectory(string baseDirectory) =>
        Path.Combine(
            Path.GetFullPath(baseDirectory),
            "models",
            "onnx-paddleocr");
}

public sealed record OnnxOcrModelFile(
    string FileName,
    Uri DownloadUri,
    string Sha256);

public static class OnnxOcrModelVerifier
{
    public static void Verify(string path, string expectedSha256)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("OCR 模型文件不存在。", path);
        }

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(
            SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(
                actual,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"OCR 模型 SHA-256 不匹配：{Path.GetFileName(path)}。");
        }
    }

    public static void VerifyAll(string modelDirectory)
    {
        foreach (var model in OnnxOcrModelCatalog.Files)
        {
            Verify(
                Path.Combine(modelDirectory, model.FileName),
                model.Sha256);
        }
    }
}
