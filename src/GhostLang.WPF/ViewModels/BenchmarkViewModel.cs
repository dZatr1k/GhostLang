using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GhostLang.Core.Benchmark;
using GhostLang.WPF.Services;
using Microsoft.Win32;

namespace GhostLang.WPF.ViewModels;

public partial class BenchmarkViewModel : ObservableObject
{
    private readonly BenchmarkRunner _runner;
    private CancellationTokenSource? _cts;
    private BenchmarkResult? _lastResult;
    private List<BenchmarkResult>? _lastBatchResults;

    [ObservableProperty] private string _samplesPath = string.Empty;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progressFraction;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private string _validationText = string.Empty;
    [ObservableProperty] private PipelineKind _lastRunKind = PipelineKind.Screen;

    public ObservableCollection<SampleResult> Results { get; } = new();

    public BenchmarkViewModel(BenchmarkRunner runner)
    {
        _runner = runner;
    }

    [RelayCommand]
    private void SelectFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = LocalizationService.Instance?["Debug_BenchmarkSelectFolder"] ?? "Select samples folder"
        };
        if (dlg.ShowDialog() == true)
            SamplesPath = dlg.FolderName;
    }

    partial void OnSamplesPathChanged(string value)
    {
        NotifyCommands();
        RefreshValidation();
    }

    private void RefreshValidation()
    {
        if (string.IsNullOrWhiteSpace(SamplesPath) || !Directory.Exists(SamplesPath))
        {
            ValidationText = string.Empty;
            return;
        }

        try
        {
            var v = _runner.ValidateSamples(SamplesPath);
            var invalids = v.Screen.Concat(v.Audio).Where(e => !e.IsValid).ToList();
            var screenValid = v.Screen.Count(e => e.IsValid);
            var audioValid = v.Audio.Count(e => e.IsValid);

            if (invalids.Count == 0)
                ValidationText = $"Screen: {screenValid} valid  ·  Audio: {audioValid} valid";
            else
                ValidationText =
                    $"Screen: {screenValid}/{v.Screen.Count} valid  ·  Audio: {audioValid}/{v.Audio.Count} valid  ·  "
                    + $"Issues: {string.Join("; ", invalids.Take(3).Select(e => $"{e.Name}: {string.Join(", ", e.Issues)}"))}"
                    + (invalids.Count > 3 ? $"  (+{invalids.Count - 3} more)" : string.Empty);
        }
        catch (Exception ex)
        {
            ValidationText = $"Validation error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task RunScreenAsync() => RunInternalAsync(PipelineKind.Screen);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task RunAudioAsync() => RunInternalAsync(PipelineKind.Audio);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task WarmUpAsync() => WarmUpInternalAsync();

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task RunScreenBatchAsync() => RunBatchInternalAsync(PipelineKind.Screen);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task RunAudioBatchAsync() => RunBatchInternalAsync(PipelineKind.Audio);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        if (_lastResult is null) return;
        var dlg = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = $"benchmark-{_lastResult.Pipeline.ToString().ToLowerInvariant()}-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        if (dlg.ShowDialog() == true)
            File.WriteAllText(dlg.FileName, BenchmarkRunner.SerializeToJson(_lastResult));
    }

    [RelayCommand(CanExecute = nameof(CanExportCsv))]
    private void ExportCsv()
    {
        var source = _lastBatchResults ?? (_lastResult is null ? null : new List<BenchmarkResult> { _lastResult });
        if (source is null) return;

        var dlg = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"benchmark-aggregate-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
        };
        if (dlg.ShowDialog() == true)
            File.WriteAllText(dlg.FileName, BenchmarkRunner.ExportAggregateCsv(source));
    }

    [RelayCommand(CanExecute = nameof(CanExportBatch))]
    private void ExportBatchJson()
    {
        if (_lastBatchResults is null) return;
        var dlg = new OpenFolderDialog { Title = "Select output folder for preset JSONs" };
        if (dlg.ShowDialog() != true) return;

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        foreach (var result in _lastBatchResults)
        {
            var fileName = $"{result.Pipeline.ToString().ToLowerInvariant()}-{result.Name}-{stamp}.json";
            var safeName = string.Concat(fileName.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_'));
            File.WriteAllText(Path.Combine(dlg.FolderName, safeName), BenchmarkRunner.SerializeToJson(result));
        }
        StatusText = $"Exported {_lastBatchResults.Count} preset results to {dlg.FolderName}";
    }

    private bool CanStart() => !IsRunning && !string.IsNullOrWhiteSpace(SamplesPath);
    private bool CanCancel() => IsRunning;
    private bool CanExport() => !IsRunning && _lastResult is not null;
    private bool CanExportCsv() => !IsRunning && (_lastResult is not null || _lastBatchResults is not null);
    private bool CanExportBatch() => !IsRunning && _lastBatchResults is not null;

    partial void OnIsRunningChanged(bool value) => NotifyCommands();

    private void NotifyCommands()
    {
        RunScreenCommand.NotifyCanExecuteChanged();
        RunAudioCommand.NotifyCanExecuteChanged();
        WarmUpCommand.NotifyCanExecuteChanged();
        RunScreenBatchCommand.NotifyCanExecuteChanged();
        RunAudioBatchCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        ExportCommand.NotifyCanExecuteChanged();
        ExportCsvCommand.NotifyCanExecuteChanged();
        ExportBatchJsonCommand.NotifyCanExecuteChanged();
    }

    private async Task WarmUpInternalAsync()
    {
        Results.Clear();
        _lastResult = null;
        _lastBatchResults = null;
        ProgressFraction = 0;
        SummaryText = string.Empty;
        StatusText = "Warming up...";
        IsRunning = true;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var progress = new Progress<string>(msg => StatusText = msg);

        try
        {
            await _runner.WarmUpScreenAsync(SamplesPath, progress, ct);
            await _runner.WarmUpAudioAsync(SamplesPath, progress, ct);
            StatusText = "Warm up done — engines loaded and cached";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Warm up cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Warm up error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            NotifyCommands();
        }
    }

    private async Task RunBatchInternalAsync(PipelineKind kind)
    {
        Results.Clear();
        _lastResult = null;
        _lastBatchResults = null;
        ProgressFraction = 0;
        SummaryText = string.Empty;
        StatusText = "Starting batch...";
        LastRunKind = kind;
        IsRunning = true;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var progress = new Progress<BenchmarkProgress>(p =>
        {
            ProgressFraction = p.FractionDone;
            StatusText = $"{p.Current}/{p.Total} — {p.SampleName}";
        });

        try
        {
            var presets = BenchmarkPresets.Ablation;
            var results = kind == PipelineKind.Screen
                ? await _runner.RunScreenBatchAsync(SamplesPath, presets, progress, ct)
                : await _runner.RunAudioBatchAsync(SamplesPath, presets, progress, ct);

            _lastBatchResults = results;

            var lines = results.Select(r =>
                $"[{r.Name}] CER {r.AverageCer:P1} · WER {r.AverageWer:P1} · BLEU {r.AverageBleu:F3} · chrF {r.AverageChrF:F3} · avg {r.AverageLatencyMs} ms");
            SummaryText = string.Join("\n", lines);
            StatusText = $"Done: {results.Count} presets × {(results.FirstOrDefault()?.Samples.Count ?? 0)} samples";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            NotifyCommands();
        }
    }

    private async Task RunInternalAsync(PipelineKind kind)
    {
        Results.Clear();
        _lastResult = null;
        _lastBatchResults = null;
        ProgressFraction = 0;
        SummaryText = string.Empty;
        StatusText = "Starting...";
        LastRunKind = kind;
        IsRunning = true;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        var progress = new Progress<BenchmarkProgress>(p =>
        {
            ProgressFraction = p.FractionDone;
            StatusText = $"{p.Current}/{p.Total} — {p.SampleName}";
        });

        try
        {
            var result = kind == PipelineKind.Screen
                ? await _runner.RunScreenAsync(SamplesPath, progress, ct)
                : await _runner.RunAudioAsync(SamplesPath, progress, ct);

            _lastResult = result;
            foreach (var sr in result.Samples) Results.Add(sr);

            SummaryText = result.Samples.Count == 0
                ? "No samples found"
                : $"Samples: {result.PassedCount}/{result.Samples.Count} passed · "
                  + $"Avg CER {result.AverageCer:P1} · Avg WER {result.AverageWer:P1} · "
                  + $"Avg latency {result.AverageLatencyMs} ms";
            StatusText = "Done";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            NotifyCommands();
        }
    }
}
