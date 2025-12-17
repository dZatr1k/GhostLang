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
}