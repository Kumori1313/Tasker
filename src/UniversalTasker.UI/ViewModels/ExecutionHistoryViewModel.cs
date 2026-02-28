using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Execution;

namespace UniversalTasker.UI.ViewModels;

public partial class ExecutionHistoryViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private DateTime _startedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyPropertyChangedFor(nameof(Duration))]
    private DateTime? _completedAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyPropertyChangedFor(nameof(ResultIcon))]
    private ExecutionState _result = ExecutionState.Running;

    public ObservableCollection<LogEntryViewModel> Entries { get; } = new();

    public string StartTimeText => StartedAt.ToString("HH:mm:ss");

    public string Duration
    {
        get
        {
            if (CompletedAt is null) return "running...";
            var elapsed = CompletedAt.Value - StartedAt;
            return elapsed.TotalSeconds < 1
                ? $"{elapsed.TotalMilliseconds:F0}ms"
                : $"{elapsed.TotalSeconds:F1}s";
        }
    }

    public string Summary => Result switch
    {
        ExecutionState.Running => "Running...",
        ExecutionState.Completed => $"Completed in {Duration}",
        ExecutionState.Cancelled => $"Stopped after {Duration}",
        ExecutionState.Failed => $"Failed after {Duration}",
        _ => Result.ToString()
    };

    public string ResultIcon => Result switch
    {
        ExecutionState.Completed => "\u2714",
        ExecutionState.Failed => "\u2716",
        ExecutionState.Cancelled => "\u25CF",
        ExecutionState.Running => "\u25B6",
        _ => "\u25CB"
    };
}
