using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using GhostLang.Core.Settings;
using TextRenderingMode = GhostLang.Core.Pipelines.Enums.TextRenderingMode;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class TextRenderingViewModel : ObservableObject, IEngineSettingsViewModel
{
    [ObservableProperty] private string _selectedFontFamily = "Arial";
    [ObservableProperty] private TextRenderingMode _renderingMode = TextRenderingMode.Compress;
    [ObservableProperty] private bool _useOriginalColor = true;
    [ObservableProperty] private string _defaultColorHex = "#FFFF00";

    public ObservableCollection<string> AvailableFonts { get; } = new();

    public Dictionary<TextRenderingMode, string> AvailableRenderingModes { get; } = BuildRenderingModes();

    private static Dictionary<TextRenderingMode, string> BuildRenderingModes()
    {
        var l = Services.LocalizationService.Instance;
        return new Dictionary<TextRenderingMode, string>
        {
            { TextRenderingMode.Compress, l?["Rendering_Compress"] ?? "Compress Width" },
            { TextRenderingMode.MatchHeight, l?["Rendering_MatchHeight"] ?? "Fixed Height" }
        };
    }

    public TextRenderingViewModel()
    {
        LoadSystemFonts();
    }

    private void LoadSystemFonts()
    {
        AvailableFonts.Clear();
        var fonts = Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x).ToList();
        foreach (var font in fonts)
        {
            AvailableFonts.Add(font);
        }
    }
    public string EngineName => "Built-in Renderer";

    public Type OptionsType => typeof(TextRenderingOptions);

    public object GetOptions()
    {
        return new TextRenderingOptions
        {
            SelectedFontFamily = SelectedFontFamily,
            RenderingMode = RenderingMode,
            UseOriginalColor = UseOriginalColor,
            DefaultColorHex = DefaultColorHex
        };
    }

    public void ApplyOptions(object options)
    {
        if (options is not TextRenderingOptions opt) return;
        
        SelectedFontFamily = opt.SelectedFontFamily;
        RenderingMode = opt.RenderingMode;
        UseOriginalColor = opt.UseOriginalColor;
        DefaultColorHex = opt.DefaultColorHex;
    }
}