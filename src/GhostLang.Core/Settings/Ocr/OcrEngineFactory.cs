using GhostLang.Core.Services.Ocr;

namespace GhostLang.Core.Settings.Ocr;

public class OcrEngineFactory(ITesseractModelManager tesseractModelManager) : IOcrEngineFactory
{
    public IOcrEngine Create(OcrEngineOptions options)
    {
        return options switch
        {
            TesseractOcrOptions tessOptions => new TesseractOcrEngine(tessOptions, tesseractModelManager),
            AzureVisionOcrOptions azure => new AzureVisionOcrEngine(azure),
            OcrSpaceOptions ocrSpace => new OcrSpaceEngine(ocrSpace),
            WindowsOcrOptions => new WindowsOcrEngine(),
            _ => throw new NotSupportedException($"Options type {options.GetType().Name} is not supported.")
        };
    }
}