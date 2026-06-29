using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;

namespace ShotLens.Windows.Ocr.Tests.Protocol;

public sealed class OcrProtocolJsonTests : IDisposable
{
    private readonly string _imagePath =
        Path.Combine(Path.GetTempPath(), $"shotlens-ocr-{Guid.NewGuid():N}.png");

    public OcrProtocolJsonTests() => File.WriteAllBytes(_imagePath, [1, 2, 3]);

    [Fact]
    public void Request_round_trips_as_camel_case_json()
    {
        var request = Request();

        var json = OcrProtocolJson.SerializeRequest(request);
        var result = OcrProtocolJson.DeserializeRequest(json);

        Assert.Contains("\"protocolVersion\":1", json);
        Assert.Equal(request.ProtocolVersion, result.ProtocolVersion);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.Equal(request.Engine, result.Engine);
        Assert.Equal(request.ImagePath, result.ImagePath);
        Assert.Equal(request.ImageSize, result.ImageSize);
        Assert.Equal(request.LanguageHints, result.LanguageHints);
    }

    [Fact]
    public void Response_round_trips_with_all_text_block_fields()
    {
        var response = Response(
            new OcrTextBlock(
                "ShotLens",
                new PhysicalRect(10, 20, 120, 32),
                0.98,
                "en",
                24,
                0.72,
                0));

        var json = OcrProtocolJson.SerializeResponse(
            response,
            new PhysicalSize(800, 600));
        var result = OcrProtocolJson.DeserializeResponse(
            json,
            new PhysicalSize(800, 600));

        Assert.Equal(response.ProtocolVersion, result.ProtocolVersion);
        Assert.Equal(response.RequestId, result.RequestId);
        Assert.Equal(response.Engine, result.Engine);
        Assert.Equal(response.EngineVersion, result.EngineVersion);
        Assert.Equal(response.ElapsedMilliseconds, result.ElapsedMilliseconds);
        Assert.Equal(response.Blocks, result.Blocks);
        Assert.Contains("\"brightness\":0.72", json);
        Assert.Contains("\"order\":0", json);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Rejects_unknown_protocol_version(int protocolVersion)
    {
        var request = Request() with { ProtocolVersion = protocolVersion };

        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.SerializeRequest(request));
    }

    [Fact]
    public void Rejects_unknown_response_protocol_version()
    {
        var response = Response() with { ProtocolVersion = 2 };

        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.SerializeResponse(
                response,
                new PhysicalSize(800, 600)));
    }

    [Fact]
    public void Rejects_missing_image()
    {
        var request = Request() with
        {
            ImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png")
        };

        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.SerializeRequest(request));
    }

    [Theory]
    [InlineData(-1, 0, 10, 10)]
    [InlineData(0, -1, 10, 10)]
    [InlineData(0, 0, 0, 10)]
    [InlineData(790, 0, 11, 10)]
    [InlineData(0, 590, 10, 11)]
    public void Rejects_invalid_block_coordinates(
        int x,
        int y,
        int width,
        int height)
    {
        var response = Response(
            new OcrTextBlock(
                "text",
                new PhysicalRect(x, y, width, height),
                0.8,
                "en",
                12,
                0.5,
                0));

        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.SerializeResponse(
                response,
                new PhysicalSize(800, 600)));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Rejects_invalid_confidence(double confidence)
    {
        var response = Response(
            new OcrTextBlock(
                "text",
                new PhysicalRect(0, 0, 10, 10),
                confidence,
                "en",
                12,
                0.5,
                0));

        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.SerializeResponse(
                response,
                new PhysicalSize(800, 600)));
    }

    [Fact]
    public void Rejects_invalid_brightness_font_size_and_order()
    {
        var valid = new OcrTextBlock(
            "text",
            new PhysicalRect(0, 0, 10, 10),
            0.8,
            "en",
            12,
            0.5,
            0);
        var invalidBlocks = new[]
        {
            valid with { Brightness = 1.1 },
            valid with { FontSizePixels = 0 },
            valid with { Order = 1 }
        };

        Assert.All(
            invalidBlocks,
            block => Assert.Throws<OcrProtocolException>(
                () => OcrProtocolJson.SerializeResponse(
                    Response(block),
                    new PhysicalSize(800, 600))));
    }

    [Fact]
    public void Rejects_corrupt_or_extra_stdout()
    {
        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.DeserializeResponse(
                "{not-json}",
                new PhysicalSize(800, 600)));
        Assert.Throws<OcrProtocolException>(
            () => OcrProtocolJson.DeserializeResponse(
                OcrProtocolJson.SerializeResponse(
                    Response(),
                    new PhysicalSize(800, 600)) + "\nextra",
                new PhysicalSize(800, 600)));
    }

    public void Dispose() => File.Delete(_imagePath);

    private OcrProtocolRequest Request() =>
        new(
            OcrProtocol.CurrentVersion,
            Guid.NewGuid().ToString("N"),
            OcrEngineIds.Fixture,
            _imagePath,
            new PhysicalSize(800, 600),
            ["en", "zh-Hans"]);

    private static OcrProtocolResponse Response(
        params OcrTextBlock[] blocks) =>
        new(
            OcrProtocol.CurrentVersion,
            Guid.NewGuid().ToString("N"),
            OcrEngineIds.Fixture,
            "fixture-1",
            42,
            blocks);
}
