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
        var effective = theme == "System" ? DetectSystemTheme() : theme;
        IsDark = effective != "Light";
        var skinUri = IsDark ? DarkSkinUri : LightSkinUri;

        var resources = Application.Current.Resources;
        var mergedDicts = resources.MergedDictionaries;

        for (var i = mergedDicts.Count - 1; i >= 0; i--)
        {
            var dict = mergedDicts[i];
            if (dict.Source == null) continue;
            var src = dict.Source.OriginalString;
            if (src.Contains("Skin") || src.Contains("Theme.xaml") || src.Contains("Overrides"))
                mergedDicts.RemoveAt(i);
        }

        mergedDicts.Insert(0, new ResourceDictionary { Source = new Uri(skinUri, UriKind.Relative) });
        mergedDicts.Insert(1, new ResourceDictionary { Source = new Uri(ThemeUri) });
        mergedDicts.Insert(2, new ResourceDictionary { Source = new Uri("Themes/Overrides.xaml", UriKind.Relative) });

        ThemeChanged?.Invoke();
    }

    public string LogoFullPath => IsDark ? "/Assets/logo-full-black-theme.svg" : "/Assets/logo-full.svg";
    public string LogoTextPath => IsDark ? "/Assets/logo-text-black-theme.svg" : "/Assets/logo-text.svg";

    private static string DetectSystemTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i && i == 1) return "Light";
            return "Dark";
        }
        catch
        {
            return "Dark";
        }
    }
}
