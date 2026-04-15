namespace GhostLang.Core.Settings.Ocr;

public class OcrSpaceOptions : OcrEngineOptions
{
    public string ApiKey { get; set; } = "";
    public int OcrEngine { get; set; } = 1;
}
