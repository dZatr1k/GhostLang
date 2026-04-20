using System.Text.Json.Serialization;

namespace GhostLang.Core.Settings.Ocr;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "EngineType")]
[JsonDerivedType(typeof(TesseractOcrOptions), "Tesseract")]
[JsonDerivedType(typeof(AzureVisionOcrOptions), "AzureVision")]
[JsonDerivedType(typeof(WindowsOcrOptions), "WindowsOcr")]
[JsonDerivedType(typeof(OcrSpaceOptions), "OcrSpace")]
public abstract class OcrEngineOptions
{

}
