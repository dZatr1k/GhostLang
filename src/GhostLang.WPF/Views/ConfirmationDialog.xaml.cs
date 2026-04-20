using System.Windows;

namespace GhostLang.WPF.Views;

public partial class ConfirmationDialog
{
    public bool Confirmed { get; private set; }

    public ConfirmationDialog(string title, string message, string yesText, string noText)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
        YesButton.Content = yesText;
        NoButton.Content = noText;
    }

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
