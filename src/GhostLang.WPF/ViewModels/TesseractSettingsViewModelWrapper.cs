namespace GhostLang.WPF.ViewModels;

public class TesseractSettingsViewModelWrapper(SettingsViewModel vm) : ViewModelBase
{
    public SettingsViewModel RealSettings { get; } = vm;
}