using System.Windows;
using GhostLang.WPF.ViewModels;

namespace GhostLang.WPF.Windows;

public partial class SettingsWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}