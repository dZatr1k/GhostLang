using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GhostLang.WPF.ViewModels.Settings;

namespace GhostLang.WPF.Controls;

public partial class LanguageMultiSelect : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<LanguageSelectionItem>),
            typeof(LanguageMultiSelect), new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string),
            typeof(LanguageMultiSelect), new PropertyMetadata("Select languages..."));

    public static readonly DependencyProperty SummaryTextProperty =
        DependencyProperty.Register(nameof(SummaryText), typeof(string),
            typeof(LanguageMultiSelect), new PropertyMetadata(""));

    public ObservableCollection<LanguageSelectionItem>? ItemsSource
    {
        get => (ObservableCollection<LanguageSelectionItem>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string SummaryText
    {
        get => (string)GetValue(SummaryTextProperty);
        set => SetValue(SummaryTextProperty, value);
    }

    public LanguageMultiSelect()
    {
        InitializeComponent();
        UpdateSummary();
    }


    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LanguageMultiSelect control)
        {
            control.CheckboxList.ItemsSource = e.NewValue as ObservableCollection<LanguageSelectionItem>;
            control.UpdateSummary();
        }
    }

    private bool _suppressToggle;

    private void DropdownToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle)
        {
            _suppressToggle = false;
            DropdownToggle.IsChecked = false;
            return;
        }
        DropdownPopup.IsOpen = true;
    }

    private void DropdownToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        DropdownPopup.IsOpen = false;
    }

    private void DropdownPopup_Closed(object? sender, EventArgs e)
    {
        if (DropdownToggle.IsMouseOver)
            _suppressToggle = true;
        DropdownToggle.IsChecked = false;
    }

    private void CheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (ItemsSource == null || ItemsSource.Count == 0)
        {
            SummaryText = Placeholder;
            return;
        }

        var selected = ItemsSource.Where(x => x.IsSelected).Select(x => x.DisplayName).ToList();
        SummaryText = selected.Count == 0 ? Placeholder : string.Join(", ", selected);
    }
}