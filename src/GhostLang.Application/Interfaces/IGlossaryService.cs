using GhostLang.Application.Models;

namespace GhostLang.Application.Interfaces;

public interface IGlossaryService
{
    Task<string> ApplyGlossary(string translatedText, IEnumerable<GlossaryRule> glossaryRules, CancellationToken cancellationToken = default);
}