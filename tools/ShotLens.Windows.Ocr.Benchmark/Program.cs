using System.IO;
using ShotLens.Windows.Core.Ocr.Benchmark;
using ShotLens.Windows.Ocr.Benchmark;

var isDatasetCommand = args.Length == 2
    && args[0] is "generate" or "validate";
var isOnnxRun = args.Length == 5
    && args[0] == "run-onnx";
if (!isDatasetCommand && !isOnnxRun)
{
    Console.Error.WriteLine(
        """
        用法：
          ShotLens.Windows.Ocr.Benchmark generate|validate <数据集目录>
          ShotLens.Windows.Ocr.Benchmark run-onnx <数据集目录> <模型基准目录> <报告目录> <发布目录>
        """);
    return 2;
}

try
{
    if (isOnnxRun)
    {
        OnnxBenchmarkRunner.Run(
            Path.GetFullPath(args[1]),
            Path.GetFullPath(args[2]),
            Path.GetFullPath(args[3]),
            Path.GetFullPath(args[4]));
    }
    else
    {
        var datasetDirectory = Path.GetFullPath(args[1]);
        if (args[0] == "generate")
        {
            PlatformDatasetGenerator.Generate(datasetDirectory);
            Console.WriteLine($"已生成 OCR 基准集：{datasetDirectory}");
        }
        else
        {
            var manifestPath = Path.Combine(datasetDirectory, "manifest.json");
            var manifest = OcrBenchmarkManifestJson.Deserialize(
                await File.ReadAllTextAsync(manifestPath));
            OcrBenchmarkDatasetValidator.Validate(manifest, datasetDirectory);
            Console.WriteLine($"OCR 基准集校验通过：{datasetDirectory}");
        }
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        $"OCR 基准命令失败：{exception.Message}");
    return 1;
}
