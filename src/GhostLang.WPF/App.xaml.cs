using System.Windows;
using GhostLang.Core.Pipelines;
using GhostLang.Core.Services;
using GhostLang.Core.Services.Asr;
using GhostLang.Core.Services.AudioCapture;
using GhostLang.Core.Services.Erasure;
using GhostLang.Core.Services.Ocr;
using GhostLang.Core.Settings.Asr;
using GhostLang.Core.Settings.Erasure;
using GhostLang.Core.Settings.Ocr;
using GhostLang.Core.Settings.Translation;
using GhostLang.WPF.Services;
using GhostLang.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GhostLang.WPF;

public partial class App : Application
{
    private readonly IHost _host;

    public T? GetService<T>() where T : class => _host.Services.GetService<T>();

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => { ConfigureServices(services); })
            .Build();

        _host.Services.GetRequiredService<LocalizationService>();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IImageTranslationPipeline, ImageTranslationPipeline>();
        services.AddSingleton<IAudioTranslationPipeline, AudioTranslationPipeline>();
        services.AddSingleton<IAsrEngineFactory, AsrEngineFactory>();
        services.AddSingleton<IAudioCaptureServiceFactory, AudioCaptureServiceFactory>();
        services.AddSingleton<IWhisperModelManager, WhisperModelManager>();
        services.AddSingleton<IAudioTranslationManager, AudioTranslationManager>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DebugViewModel>();
        
        services.AddSingleton<IOcrEngineFactory, OcrEngineFactory>();
        
        services.AddSingleton<IConfigurationService, JsonConfigurationService>();
        services.AddSingleton<IPipelineRegistry, PipelineRegistry>();
        services.AddSingleton<IPipelineBuilder, PipelineBuilder>();
        services.AddSingleton<ITranslationCacheService, TranslationCacheService>();
        services.AddSingleton<ITextErasureEngineFactory, TextErasureEngineFactory>();
        services.AddSingleton<ITesseractModelManager, TesseractModelManager>();
        services.AddSingleton<ITranslationEngineFactory, TranslationEngineFactory>();
        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
        services.AddSingleton<IScreenTranslationManager, ScreenTranslationManager>();
        services.AddSingleton<PipelineValidationService>();
        services.AddSingleton<GlobalHotKeyService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocalizationService>();

        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();

        var themeService = _host.Services.GetRequiredService<ThemeService>();
        themeService.ApplyFromConfig();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using var host = _host;
        await _host.StopAsync(TimeSpan.FromSeconds(5));
        base.OnExit(e);
    }
}