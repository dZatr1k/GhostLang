using System.Text.RegularExpressions;
using GhostLang.Core.Pipelines.Enums;
using GhostLang.Core.Pipelines.Models;

namespace GhostLang.Core.Pipelines.Steps.Implementations;

public class GlossaryTokenizationStep(List<GlossaryRule> rules, GlossaryTokenMode tokenMode) : IOptionalPipelineStep
{
    public string StepName => "Glossary Tokenization";

    public bool IsEnabled { get; set; }

    public Task ExecuteAsync(TranslationContext context, CancellationToken ct = default)
    {
        if (context.IsAborted || !IsEnabled || rules.Count == 0)
            return Task.CompletedTask;

        context.GlossaryTokenMap.Clear();
        context.GlossaryTokenMode = tokenMode;

        foreach (var fragment in context.TextFragments)
        {
            if (fragment.IsResolvedFromCache)
                continue;

            var text = fragment.OriginalText;
            var tokenIndex = context.GlossaryTokenMap.Count;

            foreach (var rule in rules)
            {
                var allForms = new List<string> { rule.SourceTerm };
                allForms.AddRange(rule.SourceVariants);

                var sorted = allForms
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Select(f => f.Trim())
                    .OrderByDescending(f => f.Length)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var form in sorted)
                {
                    var pattern = Regex.Escape(form);
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                    if (!regex.IsMatch(text)) continue;

                    var token = tokenMode switch
                    {
                        GlossaryTokenMode.HtmlTag => $"<g id=\"{tokenIndex}\">{form}</g>",
                        _ => $"<<GL_{tokenIndex}>>"
                    };

                    context.GlossaryTokenMap[$"{tokenIndex}"] = rule.TargetTerm;
                    text = regex.Replace(text, token);
                    tokenIndex++;
                    break;
                }
            }

            fragment.OriginalText = text;
        }

        return Task.CompletedTask;
    }
}
