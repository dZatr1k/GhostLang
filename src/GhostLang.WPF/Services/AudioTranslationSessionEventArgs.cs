using GhostLang.Core.Pipelines.Models;

namespace GhostLang.WPF.Services;

public class AudioTranslationSessionEventArgs : EventArgs
{
    public List<AudioFragment> Fragments { get; init; } = new();
}
