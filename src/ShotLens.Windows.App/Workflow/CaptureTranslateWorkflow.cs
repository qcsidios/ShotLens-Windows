using ShotLens.Windows.App.Capture;
using ShotLens.Windows.App.Results;
using ShotLens.Windows.Core.Ocr;
using ShotLens.Windows.Core.Settings;
using ShotLens.Windows.Core.Translation;
using ShotLens.Windows.Platform.Ocr;

namespace ShotLens.Windows.App.Workflow;

public sealed class CaptureTranslateWorkflow(
    SelectionCaptureService captureService,
    OcrWorkerClient ocrWorkerClient,
    TranslationService translationService,
    SettingsStore settingsStore,
    Func<string?> defaultApiKey)
{
    public async Task RunAsync(
        Action<string> status,
        CancellationToken cancellationToken)
    {
        status("请选择要翻译的屏幕区域。");
        var capture = captureService.CaptureSelection();
        if (capture is null)
        {
            status("已取消截图。");
            return;
        }

        status("正在识别文字…");
        var ocr = await ocrWorkerClient.RecognizeAsync(
            OcrEngineIds.PaddleSharp,
            capture.PngBytes,
            capture.Size,
            ["zh", "en"],
            cancellationToken);
        var sourceTexts = ocr.Blocks
            .OrderBy(block => block.Order)
            .Select(block => block.Text.Trim())
            .Where(text => text.Length > 0)
            .ToArray();
        if (sourceTexts.Length == 0)
        {
            ShowResult("", "", "没有识别到可翻译文字。");
            status("没有识别到可翻译文字。");
            return;
        }

        var settings = settingsStore.Load();
        var originalText = string.Join(Environment.NewLine, sourceTexts);
        try
        {
            status("正在翻译…");
            var translated = await translationService.TranslateAsync(
                sourceTexts,
                settings.TargetLanguage,
                settings.Api,
                defaultApiKey(),
                cancellationToken);
            ShowResult(
                originalText,
                string.Join(Environment.NewLine, translated.Translations),
                "翻译完成。");
            status("翻译完成。");
        }
        catch (TranslationException exception)
        {
            ShowResult(originalText, "", exception.Message);
            status(exception.Message);
        }
    }

    private static void ShowResult(
        string originalText,
        string translatedText,
        string status)
    {
        var window = new ResultWindow(originalText, translatedText, status);
        window.Show();
        window.Activate();
    }
}
