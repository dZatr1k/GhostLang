using CommunityToolkit.Mvvm.ComponentModel;

namespace GhostLang.WPF.ViewModels.Settings;

public partial class GlossaryRuleViewModel : ObservableObject
{
    [ObservableProperty]
    private string _sourceTerm = string.Empty;

    [ObservableProperty]
    private string _targetTerm = string.Empty;

    [ObservableProperty]
    private string _sourceVariants = string.Empty;
}