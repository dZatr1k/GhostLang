using System.Windows;
using GhostLang.Application.Interfaces;
using GhostLang.Infrastructure.Services;
using GhostLang.WPF.DI;
using GhostLang.WPF.UseCases;
using GhostLang.WPF.ViewModels;
using GhostLang.WPF.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace GhostLang.WPF;

public partial class App
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<TranslationUseCase>();
        
        services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
        services.AddScoped<IOcrService, OcrService>();
        services.AddScoped<IGlossaryService, GlossaryService>();
        services.AddScoped<ITranslationService, TranslationService>();

        services.AddSingleton<ITranslationContextFactory, TranslationContextFactory>();

        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SelectionWindow>();
        services.AddTransient<CaptureOverlayWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetService<MainWindow>();
        mainWindow?.Show();
    }
}