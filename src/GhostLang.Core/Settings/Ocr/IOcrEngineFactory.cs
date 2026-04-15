namespace GhostLang.Core.Settings.Ocr;

public interface IOcrEngineFactory
{
    IOcrEngine Create(OcrEngineOptions options);
}