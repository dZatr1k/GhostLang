using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Services.AudioCapture;

public class AudioTranslationSessionEventArgs : EventArgs
{
    public List<AudioFragment> Fragments { get; init; } = new();
}