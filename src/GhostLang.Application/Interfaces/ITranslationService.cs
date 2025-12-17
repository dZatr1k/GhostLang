namespace GhostLang.Application.Interfaces;

public interface ITranslationService
{
    Task<string> TranslateAsync(string textToTranslate, CancellationToken cancellationToken = default); 
}