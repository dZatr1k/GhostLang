using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Services;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Services.Ocr;
using GhostLang.Core.Settings;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Ocr;

namespace GhostLang.WPF.Services;

public class PipelineValidationService(
    IConfigurationService configService,
    ITesseractModelManager modelManager,
    IWhisperModelManager whisperModelManager)
{
    public List<string> ValidateForStart(List<SupportedLanguage> sourceLanguages)
    {
        var issues = new List<string>();
        var config = configService.Load();
        var loc = LocalizationService.Instance;

        ValidateOcrEngine(config, sourceLanguages, issues, loc);
        ValidateTranslationEngine(config, issues, loc);

        return issues;
    }

    public List<string> ValidateAudioForStart()
    {
        var issues = new List<string>();
        var config = configService.Load();
        var loc = LocalizationService.Instance;

        ValidateAsrEngine(config, issues, loc);
        ValidateTranslationEngine(config, issues, loc);

        return issues;
    }

    private void ValidateAsrEngine(AppConfig config, List<string> issues, LocalizationService? loc)
    {
        if (config.ActiveAsrEngine == null)
        {
            issues.Add(loc?["Validation_NoAsrEngine"] ?? "ASR engine is not configured.");
            return;
        }

        switch (config.ActiveAsrEngine)
        {
            case WhisperAsrOptions whisper:
                if (string.IsNullOrWhiteSpace(whisper.ModelName))
                {
                    issues.Add(loc?["Validation_WhisperNoModel"] ?? "Whisper model is not selected.");
                }
                else if (!whisperModelManager.IsModelDownloaded(whisper.ModelName, whisper.ModelsPath))
                {
                    var template = loc?["Validation_WhisperModelNotDownloaded"]
                                   ?? "Whisper model not downloaded: {0}";
                    issues.Add(string.Format(template, whisper.ModelName));
                }
                break;

            case VoskAsrOptions vosk:
                if (string.IsNullOrWhiteSpace(vosk.ModelPath))
                {
                    issues.Add(loc?["Validation_VoskNoModelSelected"]
                        ?? "No Vosk model selected. Open Settings → Vosk and pick one from the detected list.");
                }
                else if (!System.IO.Directory.Exists(vosk.ModelPath))
                {
                    var template = loc?["Validation_VoskPathMissing"]
                                   ?? "Vosk model folder does not exist: {0}";
                    issues.Add(string.Format(template, vosk.ModelPath));
                }
                else
                {

                    var required = new[] { System.IO.Path.Combine("am", "final.mdl"), System.IO.Path.Combine("conf", "model.conf") };
                    var missing = required.Where(r => !System.IO.File.Exists(System.IO.Path.Combine(vosk.ModelPath, r))).ToList();
                    if (missing.Count > 0)
                    {
                        var template = loc?["Validation_VoskModelIncomplete"]
                                       ?? "Vosk model is incomplete (missing {0}).";
                        issues.Add(string.Format(template, string.Join(", ", missing)));
                    }
                }
                break;

            case AzureAsrOptions azure:
                if (string.IsNullOrWhiteSpace(azure.ApiKey))
                    issues.Add(loc?["Validation_AzureAsrNoApiKey"] ?? "Azure Speech API key is not set.");
                if (string.IsNullOrWhiteSpace(azure.Region))
                    issues.Add(loc?["Validation_AzureAsrNoRegion"] ?? "Azure Speech region is not set.");
                break;
        }
    }

    private void ValidateOcrEngine(Core.Settings.AppConfig config, List<SupportedLanguage> sourceLanguages,
        List<string> issues, LocalizationService? loc)
    {
        if (config.ActiveOcrEngine == null)
        {
            issues.Add(loc?["Validation_NoOcrEngine"] ?? "OCR engine is not configured.");
            return;
        }

        switch (config.ActiveOcrEngine)
        {
            case TesseractOcrOptions tesseractOptions:
                var missingLangs = sourceLanguages
                    .Where(lang => !modelManager.IsModelDownloaded(lang, tesseractOptions.ModelType))
                    .ToList();

                if (missingLangs.Count > 0)
                {
                    var langNames = string.Join(", ", missingLangs);
                    var template = loc?["Validation_TesseractMissingModels"]
                                   ?? "Tesseract language models not downloaded: {0}";
                    issues.Add(string.Format(template, langNames));
                }
                break;

            case AzureVisionOcrOptions azureOptions:
                if (string.IsNullOrWhiteSpace(azureOptions.EndpointUrl))
                    issues.Add(loc?["Validation_AzureNoEndpoint"] ?? "Azure Vision endpoint URL is not set.");
                if (string.IsNullOrWhiteSpace(azureOptions.ApiKey))
                    issues.Add(loc?["Validation_AzureNoApiKey"] ?? "Azure Vision API key is not set.");
                break;

            case OcrSpaceOptions ocrSpaceOptions:
                if (string.IsNullOrWhiteSpace(ocrSpaceOptions.ApiKey))
                    issues.Add(loc?["Validation_OcrSpaceNoApiKey"] ?? "OCR.space API key is not set.");
                break;
        }
    }

    private static void ValidateTranslationEngine(Core.Settings.AppConfig config, List<string> issues,
        LocalizationService? loc)
    {
        if (config.ActiveTranslationEngine == null)
        {
            issues.Add(loc?["Validation_NoTranslationEngine"] ?? "Translation engine is not configured.");
            return;
        }

        switch (config.ActiveTranslationEngine)
        {
            case LibreTranslateOptions libreOptions:
                if (string.IsNullOrWhiteSpace(libreOptions.InstanceUrl))
                    issues.Add(loc?["Validation_LibreNoUrl"] ?? "LibreTranslate instance URL is not set.");
                break;

            case LingvaOptions lingvaOptions:
                if (string.IsNullOrWhiteSpace(lingvaOptions.InstanceUrl))
                    issues.Add(loc?["Validation_LingvaNoUrl"] ?? "Lingva instance URL is not set.");
                break;
        }
    }
}
