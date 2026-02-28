using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UniversalTasker.Core.Triggers;

namespace UniversalTasker.UI.ViewModels;

public abstract partial class TriggerViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    public abstract string DisplayName { get; }
    public abstract string TypeId { get; }

    public abstract ITrigger ToTrigger();

    public static TriggerViewModel FromTrigger(ITrigger trigger)
    {
        return trigger switch
        {
            TimerTrigger tt => new TimerTriggerViewModel
            {
                Name = tt.Name,
                IsEnabled = tt.IsEnabled,
                Interval = tt.Interval,
                FireImmediately = tt.FireImmediately
            },
            FileSystemTrigger fst => new FileSystemTriggerViewModel
            {
                Name = fst.Name,
                IsEnabled = fst.IsEnabled,
                Path = fst.Path,
                Filter = fst.Filter,
                IncludeSubdirectories = fst.IncludeSubdirectories,
                WatchCreated = fst.WatchCreated,
                WatchChanged = fst.WatchChanged,
                WatchDeleted = fst.WatchDeleted,
                WatchRenamed = fst.WatchRenamed
            },
            HotkeyTrigger ht => new HotkeyTriggerViewModel
            {
                Name = ht.Name,
                IsEnabled = ht.IsEnabled,
                VirtualKeyCode = ht.VirtualKeyCode,
                Ctrl = ht.Ctrl,
                Alt = ht.Alt,
                Shift = ht.Shift
            },
            _ => throw new ArgumentException($"Unknown trigger type: {trigger.GetType()}")
        };
    }
}

public partial class TimerTriggerViewModel : TriggerViewModel
{
    [ObservableProperty]
    private TimeSpan _interval = TimeSpan.FromMinutes(1);

    [ObservableProperty]
    private bool _fireImmediately = false;

    public override string DisplayName => "Timer";
    public override string TypeId => "timer";

    public TimerTriggerViewModel()
    {
        Name = "Timer Trigger";
    }

    public string IntervalText
    {
        get => Interval.ToString(@"hh\:mm\:ss");
        set
        {
            if (TimeSpan.TryParse(value, out var ts))
            {
                Interval = ts;
            }
        }
    }

    public override ITrigger ToTrigger()
    {
        return new TimerTrigger
        {
            Name = Name,
            IsEnabled = IsEnabled,
            Interval = Interval,
            FireImmediately = FireImmediately
        };
    }
}

public partial class FileSystemTriggerViewModel : TriggerViewModel
{
    [ObservableProperty]
    private string _path = "";

    [ObservableProperty]
    private string _filter = "*.*";

    [ObservableProperty]
    private bool _includeSubdirectories = false;

    [ObservableProperty]
    private bool _watchCreated = true;

    [ObservableProperty]
    private bool _watchChanged = true;

    [ObservableProperty]
    private bool _watchDeleted = true;

    [ObservableProperty]
    private bool _watchRenamed = true;

    public override string DisplayName => "File System";
    public override string TypeId => "filesystem";

    public FileSystemTriggerViewModel()
    {
        Name = "File System Trigger";
    }

    [RelayCommand]
    private void BrowsePath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder to watch"
        };

        if (!string.IsNullOrEmpty(Path) && System.IO.Directory.Exists(Path))
        {
            dialog.InitialDirectory = Path;
        }

        if (dialog.ShowDialog() == true)
        {
            Path = dialog.FolderName;
        }
    }

    public override ITrigger ToTrigger()
    {
        return new FileSystemTrigger
        {
            Name = Name,
            IsEnabled = IsEnabled,
            Path = Path,
            Filter = Filter,
            IncludeSubdirectories = IncludeSubdirectories,
            WatchCreated = WatchCreated,
            WatchChanged = WatchChanged,
            WatchDeleted = WatchDeleted,
            WatchRenamed = WatchRenamed
        };
    }
}

public partial class HotkeyTriggerViewModel : TriggerViewModel
{
    [ObservableProperty]
    private int _virtualKeyCode;

    [ObservableProperty]
    private bool _ctrl;

    [ObservableProperty]
    private bool _alt;

    [ObservableProperty]
    private bool _shift;

    public override string DisplayName => "Hotkey";
    public override string TypeId => "hotkey";

    public HotkeyTriggerViewModel()
    {
        Name = "Hotkey Trigger";
    }

    public string KeyDisplayText => VirtualKeyCode > 0
        ? UniversalTasker.Core.Input.VirtualKeyHelper.GetKeyDisplayString(VirtualKeyCode, Ctrl, Alt, Shift)
        : "(None)";

    public override ITrigger ToTrigger()
    {
        return new HotkeyTrigger
        {
            Name = Name,
            IsEnabled = IsEnabled,
            VirtualKeyCode = VirtualKeyCode,
            Ctrl = Ctrl,
            Alt = Alt,
            Shift = Shift
        };
    }
}
