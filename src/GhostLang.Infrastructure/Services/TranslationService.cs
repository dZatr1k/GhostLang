using GhostLang.Application.Interfaces;

namespace GhostLang.Infrastructure.Services;

public class TranslationService : ITranslationService
{
    public Task<string> TranslateAsync(string textToTranslate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Translated: {textToTranslate}");
    }
}