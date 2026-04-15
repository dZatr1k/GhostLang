namespace GhostLang.Core.Settings.Ocr;

public class AzureVisionOcrOptions : OcrEngineOptions
{
    public string EndpointUrl { get; set; } = "";
    public string ApiKey { get; set; } = "";
}
