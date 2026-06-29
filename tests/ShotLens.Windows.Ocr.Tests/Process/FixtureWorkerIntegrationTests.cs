using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Platform.Ocr;

namespace ShotLens.Windows.Ocr.Tests.Process;

public sealed class FixtureWorkerIntegrationTests
{
    [Fact]
    public async Task Fixture_engine_runs_in_an_isolated_process()
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory)
            .Parent!
            .Name;
        var executableName = OperatingSystem.IsWindows()
            ? "ShotLens.Windows.Ocr.Worker.exe"
            : "ShotLens.Windows.Ocr.Worker";
        var workerPath = Path.Combine(
            repositoryRoot,
            "src",
            "ShotLens.Windows.Ocr.Worker",
            "bin",
            configuration,
            "net8.0-windows10.0.19041.0",
            executableName);
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            $"shotlens-fixture-{Guid.NewGuid():N}");
        try
        {
            var client = new OcrWorkerClient(
                new SystemOcrWorkerProcessFactory(),
                workerPath,
                tempRoot);

            var response = await client.RecognizeAsync(
                OcrEngineIds.Fixture,
                new byte[] { 137, 80, 78, 71 },
                new PhysicalSize(800, 600),
                ["en"],
                CancellationToken.None);

            Assert.Equal(OcrEngineIds.Fixture, response.Engine);
            Assert.Equal("fixture-1", response.EngineVersion);
            Assert.Single(response.Blocks);
            Assert.Equal("Fixture", response.Blocks[0].Text);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ShotLens.Windows.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("未找到 ShotLens 仓库根目录。");
    }
}
