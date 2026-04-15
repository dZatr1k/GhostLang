using System.ComponentModel;
using System.Globalization;
using System.Resources;
using GhostLang.Core.Services;

namespace GhostLang.WPF.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService? Instance { get; private set; }

    private readonly ResourceManager _resourceManager = new("GhostLang.WPF.Properties.Strings",
        typeof(LocalizationService).Assembly);

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage { get; private set; } = "ru";

    public LocalizationService(IConfigurationService configService)
    {
        Instance = this;
        var config = configService.Load();
        Apply(config.Language);
    }

    public void Apply(string language)
    {
        CurrentLanguage = language;
        var culture = new CultureInfo(language);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string this[string key] => _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}