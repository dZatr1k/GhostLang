using System.Windows.Controls;
using System.Windows.Input;
using GhostLang.WPF.ViewModels;
using GhostLang.WPF.ViewModels.Settings;

namespace GhostLang.WPF.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void HotKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotKeyBindingViewModel vm }) return;
        if (!vm.IsRecording) return;

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        vm.ApplyRecordedKey(key, Keyboard.Modifiers);
    }

    private void HotKeyTextBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotKeyBindingViewModel vm }) return;
        (DataContext as SettingsViewModel)?.SuspendHotKeys();
        vm.StartRecordingCommand.Execute(null);
    }

    private void HotKeyTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: HotKeyBindingViewModel vm }) return;
        vm.CancelRecording();
        (DataContext as SettingsViewModel)?.ResumeHotKeys();
    }
}
