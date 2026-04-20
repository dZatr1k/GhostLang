using System.Text.Json.Serialization;

namespace GhostLang.Core.Settings.Translation;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "EngineType")]
[JsonDerivedType(typeof(GTranslateOptions), "GTranslate")]
[JsonDerivedType(typeof(MyMemoryOptions), "MyMemory")]
[JsonDerivedType(typeof(LingvaOptions), "Lingva")]
[JsonDerivedType(typeof(LibreTranslateOptions), "LibreTranslate")]
public abstract class TranslationEngineOptions
{
}
