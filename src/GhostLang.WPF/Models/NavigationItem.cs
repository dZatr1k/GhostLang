using System.Collections.ObjectModel;
using GhostLang.WPF.ViewModels;

namespace GhostLang.WPF.Models;

public class NavigationItem
{
    public string Title { get; set; }
    
    public ViewModelBase? DestinationViewModel { get; set; }
    
    public ObservableCollection<NavigationItem> Children { get; set; } = new();
    
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; } = true;
}