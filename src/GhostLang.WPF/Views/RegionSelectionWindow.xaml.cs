using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GhostLang.Core.Services;
using GhostLang.WPF.Services;

namespace GhostLang.WPF.Views;

public partial class RegionSelectionWindow : Window
{
    private Point _startPoint;
    private bool _isDragging;

    public CaptureRegion? SelectedRegion { get; private set; }

    public RegionSelectionWindow()
    {
        InitializeComponent();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _startPoint = e.GetPosition(SelectionCanvas);
        _isDragging = true;

        SelectionRect.Visibility = Visibility.Visible;
        SizeLabel.Visibility = Visibility.Visible;

        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;

        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isDragging) return;

        var currentPoint = e.GetPosition(SelectionCanvas);

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(currentPoint.X - _startPoint.X);
        var height = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;

        var (physW, physH) = DpiHelper.LogicalSizeToPhysical(width, height, this);
        SizeLabel.Text = $"{physW} x {physH} px";
        Canvas.SetLeft(SizeLabel, x);
        Canvas.SetTop(SizeLabel, y - 22);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!_isDragging) return;

        _isDragging = false;
        ReleaseMouseCapture();

        var currentPoint = e.GetPosition(SelectionCanvas);

        var logicalX = Math.Min(_startPoint.X, currentPoint.X);
        var logicalY = Math.Min(_startPoint.Y, currentPoint.Y);
        var logicalW = Math.Abs(currentPoint.X - _startPoint.X);
        var logicalH = Math.Abs(currentPoint.Y - _startPoint.Y);

        if (logicalW < 10 || logicalH < 10)
        {
            SelectedRegion = null;
            DialogResult = false;
            return;
        }

        var (physX, physY) = DpiHelper.LogicalToPhysical(logicalX + Left, logicalY + Top, this);
        var (physW, physH) = DpiHelper.LogicalSizeToPhysical(logicalW, logicalH, this);

        SelectedRegion = new CaptureRegion
        {
            X = physX,
            Y = physY,
            Width = physW,
            Height = physH
        };

        DialogResult = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            DialogResult = false;
        }
    }
}
