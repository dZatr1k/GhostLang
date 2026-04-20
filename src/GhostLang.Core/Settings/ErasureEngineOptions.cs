using System.Text.Json.Serialization;
using GhostLang.Core.Settings.Erasure;

namespace GhostLang.Core.Settings;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "EngineType")]
[JsonDerivedType(typeof(SolidColorErasureOptions), typeDiscriminator: "SolidColor")]
[JsonDerivedType(typeof(OpenCvErasureOptions), typeDiscriminator: "OpenCv")]
public abstract class ErasureEngineOptions
{
}
