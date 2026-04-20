using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace GhostLang.WPF.Services;

public static class RichTextBoxHelpers
{
    public static readonly DependencyProperty DocumentProperty = DependencyProperty.RegisterAttached(
        "Document",
        typeof(FlowDocument),
        typeof(RichTextBoxHelpers),
        new PropertyMetadata(null, OnDocumentChanged));

    public static void SetDocument(DependencyObject element, FlowDocument value)
        => element.SetValue(DocumentProperty, value);

    public static FlowDocument GetDocument(DependencyObject element)
        => (FlowDocument)element.GetValue(DocumentProperty);

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox rtb) return;

        if (e.NewValue is not FlowDocument doc)
        {
            rtb.Document = new FlowDocument();
            return;
        }

        if (doc.Parent is RichTextBox previousOwner && !ReferenceEquals(previousOwner, rtb))
            previousOwner.Document = new FlowDocument();

        rtb.Document = doc;
    }
}
