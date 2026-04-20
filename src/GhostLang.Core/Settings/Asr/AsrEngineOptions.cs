using System.Text.Json.Serialization;

namespace GhostLang.Core.Settings.Asr;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "EngineType")]
[JsonDerivedType(typeof(WhisperAsrOptions), "Whisper")]
[JsonDerivedType(typeof(VoskAsrOptions), "Vosk")]
[JsonDerivedType(typeof(AzureAsrOptions), "Azure")]
public abstract class AsrEngineOptions
{

}
