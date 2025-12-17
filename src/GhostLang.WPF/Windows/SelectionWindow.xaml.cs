using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GhostLang.WPF.Windows;

public partial class SelectionWindow
{
    private Point _startPoint;
    public Rect SelectedArea { get; private set; }

    public SelectionWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, _startPoint.X);
        Canvas.SetTop(SelectionBorder, _startPoint.Y);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (IsMouseCaptured)
        {
            var currentPoint = e.GetPosition(this);

            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(_startPoint.X - currentPoint.X);
            var height = Math.Abs(_startPoint.Y - currentPoint.Y);

            Canvas.SetLeft(SelectionBorder, x);
            Canvas.SetTop(SelectionBorder, y);
            SelectionBorder.Width = width;
            SelectionBorder.Height = height;
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        SelectionBorder.Visibility = Visibility.Collapsed;

        var endPoint = e.GetPosition(this);
        var x = Math.Min(_startPoint.X, endPoint.X);
        var y = Math.Min(_startPoint.Y, endPoint.Y);
        var width = Math.Abs(_startPoint.X - endPoint.X);
        var height = Math.Abs(_startPoint.Y - endPoint.Y);

        var relativeTopLeft = new Point(x, y);
            
        var screenTopLeft = PointToScreen(relativeTopLeft);

        SelectedArea = new Rect(screenTopLeft.X, screenTopLeft.Y, width, height);

        DialogResult = SelectedArea is { Width: > 10, Height: > 10 };
        
        Close();
    }
}