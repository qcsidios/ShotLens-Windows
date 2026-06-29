using System.Text.Json;
using System.Text.Json.Serialization;
using ShotLens.Windows.Core.Capture;

namespace ShotLens.Windows.Core.Ocr;

public static class OcrProtocolJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static string SerializeRequest(OcrProtocolRequest request)
    {
        ValidateRequest(request);
        return JsonSerializer.Serialize(request, Options);
    }

    public static OcrProtocolRequest DeserializeRequest(string json)
    {
        var request = Deserialize<OcrProtocolRequest>(json);
        ValidateRequest(request);
        return request;
    }

    public static string SerializeResponse(
        OcrProtocolResponse response,
        PhysicalSize imageSize)
    {
        ValidateResponse(response, imageSize);
        return JsonSerializer.Serialize(response, Options);
    }

    public static OcrProtocolResponse DeserializeResponse(
        string json,
        PhysicalSize imageSize)
    {
        var response = Deserialize<OcrProtocolResponse>(json);
        ValidateResponse(response, imageSize);
        return response;
    }

    private static T Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new OcrProtocolException("OCR JSON 不能为空。");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new OcrProtocolException("OCR JSON 不能为 null。");
        }
        catch (JsonException exception)
        {
            throw new OcrProtocolException("OCR JSON 格式无效。", exception);
        }
    }

    private static void ValidateRequest(OcrProtocolRequest request)
    {
        ValidateVersion(request.ProtocolVersion);
        ValidateRequestId(request.RequestId);
        ValidateEngine(request.Engine);
        ValidateImageSize(request.ImageSize);
        if (string.IsNullOrWhiteSpace(request.ImagePath)
            || !Path.IsPathFullyQualified(request.ImagePath)
            || !File.Exists(request.ImagePath))
        {
            throw new OcrProtocolException("OCR 请求图片不存在或不是绝对路径。");
        }

        if (request.LanguageHints is null
            || request.LanguageHints.Any(string.IsNullOrWhiteSpace))
        {
            throw new OcrProtocolException("OCR 语言提示无效。");
        }
    }

    private static void ValidateResponse(
        OcrProtocolResponse response,
        PhysicalSize imageSize)
    {
        ValidateVersion(response.ProtocolVersion);
        ValidateRequestId(response.RequestId);
        ValidateEngine(response.Engine);
        ValidateImageSize(imageSize);
        if (string.IsNullOrWhiteSpace(response.EngineVersion))
        {
            throw new OcrProtocolException("OCR 引擎版本不能为空。");
        }

        if (response.ElapsedMilliseconds < 0)
        {
            throw new OcrProtocolException("OCR 耗时不能为负数。");
        }

        if (response.Blocks is null)
        {
            throw new OcrProtocolException("OCR 文本块不能为空。");
        }

        var orders = new HashSet<int>();
        foreach (var block in response.Blocks)
        {
            ValidateBlock(block, imageSize);
            if (!orders.Add(block.Order)
                || block.Order < 0
                || block.Order >= response.Blocks.Length)
            {
                throw new OcrProtocolException("OCR 文本块顺序无效。");
            }
        }
    }

    private static void ValidateBlock(
        OcrTextBlock block,
        PhysicalSize imageSize)
    {
        if (string.IsNullOrWhiteSpace(block.Text))
        {
            throw new OcrProtocolException("OCR 文本块内容不能为空。");
        }

        if (block.Bounds.X < 0
            || block.Bounds.Y < 0
            || block.Bounds.Width <= 0
            || block.Bounds.Height <= 0
            || (long)block.Bounds.X + block.Bounds.Width > imageSize.Width
            || (long)block.Bounds.Y + block.Bounds.Height > imageSize.Height)
        {
            throw new OcrProtocolException("OCR 文本块坐标超出图片。");
        }

        if (!IsUnitValue(block.Confidence))
        {
            throw new OcrProtocolException("OCR 置信度必须位于 0 到 1。");
        }

        if (string.IsNullOrWhiteSpace(block.Language))
        {
            throw new OcrProtocolException("OCR 文本块语言不能为空。");
        }

        if (!double.IsFinite(block.FontSizePixels)
            || block.FontSizePixels <= 0)
        {
            throw new OcrProtocolException("OCR 字号必须为正数。");
        }

        if (!IsUnitValue(block.Brightness))
        {
            throw new OcrProtocolException("OCR 亮度必须位于 0 到 1。");
        }
    }

    private static void ValidateVersion(int protocolVersion)
    {
        if (protocolVersion != OcrProtocol.CurrentVersion)
        {
            throw new OcrProtocolException(
                $"不支持 OCR 协议版本 {protocolVersion}。");
        }
    }

    private static void ValidateRequestId(string requestId)
    {
        if (!Guid.TryParseExact(requestId, "N", out _))
        {
            throw new OcrProtocolException("OCR requestId 必须为 N 格式 GUID。");
        }
    }

    private static void ValidateEngine(string engine)
    {
        if (string.IsNullOrWhiteSpace(engine)
            || !OcrEngineIds.IsKnown(engine))
        {
            throw new OcrProtocolException($"未知 OCR 引擎：{engine}。");
        }
    }

    private static void ValidateImageSize(PhysicalSize imageSize)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            throw new OcrProtocolException("OCR 图片尺寸必须为正数。");
        }
    }

    private static bool IsUnitValue(double value) =>
        double.IsFinite(value) && value is >= 0 and <= 1;
}
