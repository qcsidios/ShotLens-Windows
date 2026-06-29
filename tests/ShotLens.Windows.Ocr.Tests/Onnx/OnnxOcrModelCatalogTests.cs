using System.Security.Cryptography;
using System.Text;
using ShotLens.Windows.Ocr.Worker.Onnx;

namespace ShotLens.Windows.Ocr.Tests.Onnx;

public sealed class OnnxOcrModelCatalogTests
{
    [Fact]
    public void Catalog_locks_pp_ocr_v5_mobile_urls_and_hashes()
    {
        Assert.Equal("RapidOCR-v3.9.0/PP-OCRv5", OnnxOcrModelCatalog.Version);
        Assert.Collection(
            OnnxOcrModelCatalog.Files,
            file => AssertModel(
                file,
                "ch_PP-OCRv5_mobile_det.onnx",
                "4d97c44a20d30a81aad087d6a396b08f786c4635742afc391f6621f5c6ae78ae"),
            file => AssertModel(
                file,
                "ch_ppocr_mobile_v2.0_cls_mobile.onnx",
                "e47acedf663230f8863ff1ab0e64dd2d82b838fceb5957146dab185a89d6215c"),
            file => AssertModel(
                file,
                "ch_PP-OCRv5_rec_mobile.onnx",
                "5825fc7ebf84ae7a412be049820b4d86d77620f204a041697b0494669b1742c5"),
            file => AssertModel(
                file,
                "ppocrv5_dict.txt",
                "d1979e9f794c464c0d2e0b70a7fe14dd978e9dc644c0e71f14158cdf8342af1b"));
    }

    [Fact]
    public void Verifier_accepts_matching_sha_256()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "shotlens", Encoding.UTF8);
            var expected = Convert.ToHexString(
                SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

            OnnxOcrModelVerifier.Verify(path, expected);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Verifier_rejects_missing_or_mismatched_model()
    {
        var missing = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.onnx");
        var existing = Path.GetTempFileName();
        try
        {
            Assert.Throws<FileNotFoundException>(
                () => OnnxOcrModelVerifier.Verify(missing, new string('0', 64)));
            Assert.Throws<InvalidDataException>(
                () => OnnxOcrModelVerifier.Verify(existing, new string('0', 64)));
        }
        finally
        {
            File.Delete(existing);
        }
    }

    [Fact]
    public void Download_script_checks_hash_before_atomic_rename()
    {
        var script = File.ReadAllText(
            RepositoryFile("scripts", "download-onnx-ocr-models.ps1"));

        Assert.Contains("Get-FileHash", script);
        Assert.Contains("-Algorithm SHA256", script);
        Assert.Contains(".partial", script);
        Assert.Contains("Move-Item", script);
        foreach (var model in OnnxOcrModelCatalog.Files)
        {
            Assert.Contains(model.Sha256, script);
        }
    }

    private static void AssertModel(
        OnnxOcrModelFile model,
        string expectedFileName,
        string expectedSha256)
    {
        Assert.Equal(expectedFileName, model.FileName);
        Assert.Equal(expectedSha256, model.Sha256);
        Assert.Equal(Uri.UriSchemeHttps, model.DownloadUri.Scheme);
        Assert.Equal("www.modelscope.cn", model.DownloadUri.Host);
    }

    private static string RepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solution = Path.Combine(directory.FullName, "ShotLens.Windows.sln");
            if (File.Exists(solution))
            {
                return Path.Combine([directory.FullName, .. parts]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("未找到 ShotLens 仓库根目录。");
    }
}
