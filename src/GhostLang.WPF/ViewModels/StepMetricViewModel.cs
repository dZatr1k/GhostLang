using System.Windows;

namespace GhostLang.WPF.ViewModels;

public class StepMetricViewModel
{
    public int Order { get; init; }
    public string StepName { get; init; } = string.Empty;
    public long ElapsedMilliseconds { get; init; }
    public double Percentage { get; init; }
    public double BarWidth { get; init; }

    public GridLength FilledColumn => new(System.Math.Max(0.0001, BarWidth), GridUnitType.Star);
    public GridLength EmptyColumn => new(System.Math.Max(0.0001, 1.0 - BarWidth), GridUnitType.Star);

    public bool IsZero => ElapsedMilliseconds == 0;
}
