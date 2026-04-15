using System.Text.RegularExpressions;
using GhostLang.Core.Pipelines.Enums;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class GlossaryRestorationStep : IOptionalPipelineStep
{
    public bool IsEnabled { get; set; } = true;

    public string StepName => "Glossary Restoration";

    public Task ExecuteAsync(TranslationContext context)
    {
        if (context.IsAborted || !IsEnabled || context.GlossaryTokenMap.Count == 0)
            return Task.CompletedTask;

        foreach (var fragment in context.TextFragments)
        {
            if (string.IsNullOrWhiteSpace(fragment.TranslatedText))
                continue;

            var text = fragment.TranslatedText;

            if (context.GlossaryTokenMode == GlossaryTokenMode.HtmlTag)
            {
                text = Regex.Replace(text, @"<g\s+id=""(\d+)"">(.*?)</g>", match =>
                {
                    var id = match.Groups[1].Value;
                    return context.GlossaryTokenMap.TryGetValue(id, out var translation)
                        ? translation
                        : match.Value;
                }, RegexOptions.IgnoreCase);
            }
            else
            {
                text = Regex.Replace(text, @"<<GL_(\d+)>>", match =>
                {
                    var id = match.Groups[1].Value;
                    return context.GlossaryTokenMap.TryGetValue(id, out var translation)
                        ? translation
                        : match.Value;
                }, RegexOptions.IgnoreCase);
            }

            fragment.TranslatedText = text;
        }

        return Task.CompletedTask;
    }
}