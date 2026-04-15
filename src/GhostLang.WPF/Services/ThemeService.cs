using System.Windows;
using GhostLang.Core.Services;

namespace GhostLang.WPF.Services;

public class ThemeService(IConfigurationService configService)
{
    private const string DarkSkinUri = "Themes/SkinDark.xaml";
    private const string LightSkinUri = "Themes/SkinLight.xaml";
    private const string ThemeUri = "pack://application:,,,/HandyControl;component/Themes/Theme.xaml";

    public bool IsDark { get; private set; } = true;

    public event Action? ThemeChanged;

    public void ApplyFromConfig()
    {
        var config = configService.Load();
        Apply(config.Theme);
    }

    public void Apply(string theme)
    {
        IsDark = theme != "Light";
        var skinUri = IsDark ? DarkSkinUri : LightSkinUri;

        var resources = Application.Current.Resources;
        var mergedDicts = resources.MergedDictionaries;

        // Remove Skin, Theme, and Overrides dictionaries
        for (var i = mergedDicts.Count - 1; i >= 0; i--)
        {
            var dict = mergedDicts[i];
            if (dict.Source == null) continue;
            var src = dict.Source.OriginalString;
            if (src.Contains("Skin") || src.Contains("Theme.xaml") || src.Contains("Overrides"))
                mergedDicts.RemoveAt(i);
        }

        // Re-add in correct order: Skin, Theme, Overrides
        mergedDicts.Insert(0, new ResourceDictionary { Source = new Uri(skinUri, UriKind.Relative) });
        mergedDicts.Insert(1, new ResourceDictionary { Source = new Uri(ThemeUri) });
        mergedDicts.Insert(2, new ResourceDictionary { Source = new Uri("Themes/Overrides.xaml", UriKind.Relative) });

        ThemeChanged?.Invoke();
    }

    public string LogoFullPath => IsDark ? "/Assets/logo-full-black-theme.svg" : "/Assets/logo-full.svg";
    public string LogoTextPath => IsDark ? "/Assets/logo-text-black-theme.svg" : "/Assets/logo-text.svg";
}