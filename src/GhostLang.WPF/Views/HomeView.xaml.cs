using System.Windows.Controls;
using GhostLang.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GhostLang.WPF.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            var themeService = ((App)System.Windows.Application.Current).GetService<ThemeService>();
            if (themeService == null) return;

            UpdateLogo(themeService);
            themeService.ThemeChanged += () => UpdateLogo(themeService);
        };
    }

    private void UpdateLogo(ThemeService themeService)
    {
        LogoImage.Source = new Uri(themeService.LogoFullPath, UriKind.Relative);
    }
}