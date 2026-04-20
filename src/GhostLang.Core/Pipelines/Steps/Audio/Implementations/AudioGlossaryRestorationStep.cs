using System.Text.RegularExpressions;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines.Steps.Audio.Implementations;

public class AudioGlossaryRestorationStep : IOptionalAudioPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    public Task ExecuteAsync(AudioTranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || context.GlossaryTokenMap.Count == 0)
            return Task.CompletedTask;

        foreach (var fragment in context.AudioFragments)
        {
            if (string.IsNullOrWhiteSpace(fragment.TranslatedText))
                continue;

            var text = fragment.TranslatedText;

            if (context.GlossaryTokenMode == GlossaryTokenMode.HtmlTag)
            {
                text = Regex.Replace(text, @"<g\s+[^>]*?id=[""'](\d+)[""'][^>]*>(.*?)</g>", match =>
                {
                    var id = match.Groups[1].Value;
                    if (context.GlossaryTokenMap.TryGetValue(id, out var translation))
                        return translation;
                    return match.Groups[2].Value;
                }, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else
            {
                text = Regex.Replace(text, @"<<GL_(\d+)>>", match =>
                {
                    var id = match.Groups[1].Value;
                    if (context.GlossaryTokenMap.TryGetValue(id, out var translation))
                        return translation;
                    return string.Empty;
                }, RegexOptions.IgnoreCase);
            }

            fragment.TranslatedText = text;
        }

        return Task.CompletedTask;
    }
}
