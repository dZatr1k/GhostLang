using System.Windows;
using System.Windows.Interop;
using GhostLang.WPF.Helpers;
using GhostLang.WPF.Models;
using GhostLang.WPF.ViewModels;

namespace GhostLang.WPF.Windows;

public partial class MainWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void MainMenu_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainWindowViewModel vm && e.NewValue is NavigationItem item)
        {
            if (item.DestinationViewModel != null)
            {
                vm.CurrentView = item.DestinationViewModel;
            }
        }
    }
}