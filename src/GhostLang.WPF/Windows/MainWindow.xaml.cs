using GhostLang.WPF.ViewModels;

namespace GhostLang.WPF.Windows;

public partial class MainWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}