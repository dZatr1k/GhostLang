namespace GhostLang.WPF.ViewModels.Settings;

public interface IEngineSettingsViewModel
{
    string EngineName { get; }

    Type OptionsType { get; }

    object GetOptions();
    void ApplyOptions(object options);
}
