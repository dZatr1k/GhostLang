using GhostLang.Core.Settings.ImagePreProcessing;

namespace GhostLang.WPF.ViewModels.Settings;

public class FilterViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public FilterOption Option { get; set; } = null!;

    public bool HasParameter { get; set; } = true;
}