using GhostLang.Application.Interfaces;
using GhostLang.Infrastructure.Services;
using GhostLang.WPF.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace GhostLang.WPF.DI;

public class TranslationContextFactory(IServiceProvider serviceProvider) : ITranslationContextFactory
{
    public ITranslationContext CreateContext()
    {
        var scope = serviceProvider.CreateScope();
        return new TranslationContext(scope);
    }
}

file class TranslationContext(IServiceScope scope) : ITranslationContext
{
    public TranslationUseCase TranslationUseCase { get; } = scope.ServiceProvider.GetRequiredService<TranslationUseCase>();
    public IScreenCaptureService ScreenCaptureService { get; } = scope.ServiceProvider.GetRequiredService<IScreenCaptureService>();

    public void Dispose()
    {
        scope.Dispose();
    }
}
