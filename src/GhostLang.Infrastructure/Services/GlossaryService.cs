using GhostLang.Application.Interfaces;
using GhostLang.Application.Models;

namespace GhostLang.Infrastructure.Services;

public class GlossaryService : IGlossaryService
{
    public Task<string> ApplyGlossary(string translatedText, IEnumerable<GlossaryRule> glossaryRules, CancellationToken cancellationToken = default)
    {
        foreach (var rule in glossaryRules)
        {
            translatedText = translatedText.Replace(rule.SourceTerm, rule.TargetTerm, StringComparison.OrdinalIgnoreCase);
        }

        return Task.FromResult(translatedText);
    }
}