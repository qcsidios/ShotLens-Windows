using System.Diagnostics;
using ShotLens.Windows.Core.Capture;
using ShotLens.Windows.Core.Ocr;

try
{
    var requestJson = await Console.In.ReadToEndAsync();
    var request = OcrProtocolJson.DeserializeRequest(requestJson);
    if (request.Engine != OcrEngineIds.Fixture)
    {
        Console.Error.WriteLine("ocr_worker_engine_unavailable");
        return 3;
    }

    var stopwatch = Stopwatch.StartNew();
    var response = new OcrProtocolResponse(
        OcrProtocol.CurrentVersion,
        request.RequestId,
        OcrEngineIds.Fixture,
        "fixture-1",
        stopwatch.ElapsedMilliseconds,
        [
            new OcrTextBlock(
                "Fixture",
                new PhysicalRect(
                    0,
                    0,
                    Math.Min(100, request.ImageSize.Width),
                    Math.Min(30, request.ImageSize.Height)),
                1,
                request.LanguageHints.FirstOrDefault() ?? "und",
                Math.Min(16, request.ImageSize.Height),
                0.5,
                0)
        ]);
    Console.Out.Write(
        OcrProtocolJson.SerializeResponse(response, request.ImageSize));
    Console.Error.WriteLine("ocr_worker_completed;blocks=1");
    return 0;
}
catch (OcrProtocolException)
{
    Console.Error.WriteLine("ocr_worker_protocol_error");
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine(
        $"ocr_worker_failed;type={exception.GetType().Name}");
    return 1;
}
