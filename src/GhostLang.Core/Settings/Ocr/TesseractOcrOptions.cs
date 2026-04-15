using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Settings.Ocr;

public class TesseractOcrOptions : OcrEngineOptions
{
    public TesseractModelType ModelType { get; set; } = TesseractModelType.Fast;
    
    public List<SupportedLanguage> SourceLanguages { get; set; } = [];
}