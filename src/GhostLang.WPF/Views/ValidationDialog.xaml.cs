using System.Collections.Generic;
using System.Windows;

namespace GhostLang.WPF.Views;

public partial class ValidationDialog
{
    public bool OpenSettingsRequested { get; private set; }

    public ValidationDialog(List<string> issues)
    {
        InitializeComponent();
        IssuesList.ItemsSource = issues;
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsRequested = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}