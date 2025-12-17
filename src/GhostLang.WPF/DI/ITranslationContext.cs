using GhostLang.Application.Interfaces;
using GhostLang.WPF.UseCases;

namespace GhostLang.WPF.DI;

public interface ITranslationContext : IDisposable
{
    TranslationUseCase TranslationUseCase { get; }
    IScreenCaptureService ScreenCaptureService { get; }
}