using GhostLang.Application.Models;

namespace GhostLang.Application.Interfaces;

public interface IGlossaryRepository
{
    Task<IEnumerable<GlossaryRule>> GetRules(CancellationToken cancellationToken = default);
}