using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Execution;
using UniversalTasker.Core.Input;
using UniversalTasker.Core.Triggers;
using UniversalTasker.Core.Workflows;
using UniversalTasker.Serialization;
using UniversalTasker.UI.Services;

namespace UniversalTasker.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ExecutionEngine _engine = new();
    private readonly HotkeyService _hotkeyService = new();
    private readonly WorkflowSerializer _serializer = new();
    private readonly UndoManager _undoManager = new();
    private GlobalKeyboardHook? _hotkeyCapture;
    private string? _currentFilePath;

    public ObservableCollection<ActionViewModel> Actions { get; } = new();
    public ObservableCollection<TriggerViewModel> Triggers { get; } = new();
    public ObservableCollection<VariableItemViewModel> Variables { get; } = new();

    // Execution History
    public ObservableCollection<ExecutionHistoryViewModel> ExecutionHistory { get; } = new();
    private ExecutionHistoryViewModel? _currentExecution;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveActionCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
    private ActionViewModel? _selectedAction;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTriggerCommand))]
    private TriggerViewModel? _selectedTrigger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveVariableCommand))]
    private VariableItemViewModel? _selectedVariable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _workflowName = "New Workflow";

    [ObservableProperty]
    private bool _isLogPanelVisible = true;

    [ObservableProperty]
    private ExecutionHistoryViewModel? _selectedExecution;

    // Hotkey properties
    [ObservableProperty]
    private HotkeyBinding _startHotkey = new(0x75, false, false, false); // F6

    [ObservableProperty]
    private HotkeyBinding _stopHotkey = new(0x76, false, false, false); // F7

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSeparateStopHotkey))]
    private bool _useSameHotkey = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetStartHotkeyCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetStopHotkeyCommand))]
    private bool _isCapturingHotkey;

    [ObservableProperty]
    private string _capturingHotkeyLabel = "";

    public bool ShowSeparateStopHotkey => !UseSameHotkey;

    // Undo/Redo expose for binding
    public UndoManager UndoManager => _undoManager;

    public MainViewModel()
    {
        _engine.ExecutionStarted += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsRunning = true;
                StatusText = "Running...";

                _currentExecution = new ExecutionHistoryViewModel
                {
                    StartedAt = DateTime.Now
                };
                ExecutionHistory.Insert(0, _currentExecution);
                SelectedExecution = _currentExecution;
                _currentExecution.Entries.Add(new LogEntryViewModel
                {
                    Timestamp = DateTime.Now,
                    Level = "Info",
                    Message = "Execution started"
                });
            });
        };

        _engine.ActionExecuting += (_, args) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = $"Executing: {args.Action.Name} ({args.Index + 1}/{args.TotalCount})";

                _currentExecution?.Entries.Add(new LogEntryViewModel
                {
                    Timestamp = DateTime.Now,
                    Level = "Info",
                    Message = $"[{args.Index + 1}/{args.TotalCount}] Executing: {args.Action.Name}"
                });
            });
        };

        _engine.ExecutionCompleted += (_, args) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsRunning = false;
                StatusText = args.FinalState switch
                {
                    ExecutionState.Completed => "Completed",
                    ExecutionState.Cancelled => "Stopped",
                    ExecutionState.Failed => $"Failed: {args.Exception?.Message}",
                    _ => "Ready"
                };

                if (_currentExecution != null)
                {
                    _currentExecution.CompletedAt = DateTime.Now;
                    _currentExecution.Result = args.FinalState;

                    var level = args.FinalState == ExecutionState.Failed ? "Error"
                        : args.FinalState == ExecutionState.Cancelled ? "Warning"
                        : "Info";

                    var message = args.FinalState == ExecutionState.Failed
                        ? $"Execution failed: {args.Exception?.Message}"
                        : $"Execution {args.FinalState.ToString().ToLowerInvariant()}";

                    _currentExecution.Entries.Add(new LogEntryViewModel
                    {
                        Timestamp = DateTime.Now,
                        Level = level,
                        Message = message
                    });

                    _currentExecution = null;
                }
            });
        };

        // Set up hotkey service
        _hotkeyService.StartHotkey = StartHotkey;
        _hotkeyService.StopHotkey = StopHotkey;
        _hotkeyService.UseSameHotkey = UseSameHotkey;

        _hotkeyService.StartTriggered += async (_, _) =>
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (UseSameHotkey)
                {
                    // Toggle behavior
                    if (IsRunning)
                        Stop();
                    else if (CanStart())
                        await Start();
                }
                else
                {
                    if (CanStart())
                        await Start();
                }
            });
        };

        _hotkeyService.StopTriggered += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CanStop())
                    Stop();
            });
        };

        _hotkeyService.Start();
    }

    partial void OnStartHotkeyChanged(HotkeyBinding value)
    {
        _hotkeyService.StartHotkey = value;
    }

    partial void OnStopHotkeyChanged(HotkeyBinding value)
    {
        _hotkeyService.StopHotkey = value;
    }

    partial void OnUseSameHotkeyChanged(bool value)
    {
        _hotkeyService.UseSameHotkey = value;
    }

    // Undo/Redo Commands
    [RelayCommand]
    private void Undo() => _undoManager.Undo();

    [RelayCommand]
    private void Redo() => _undoManager.Redo();

    // File Operations
    [RelayCommand]
    private void NewWorkflow()
    {
        Actions.Clear();
        Triggers.Clear();
        Variables.Clear();
        WorkflowName = "New Workflow";
        _currentFilePath = null;
        _undoManager.Clear();
        StatusText = "New workflow created";
    }

    [RelayCommand]
    private async Task SaveWorkflow()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            await SaveWorkflowAs();
            return;
        }

        await SaveToFile(_currentFilePath);
    }

    [RelayCommand]
    private async Task SaveWorkflowAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON Workflow (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = WorkflowName
        };

        if (dialog.ShowDialog() == true)
        {
            _currentFilePath = dialog.FileName;
            await SaveToFile(_currentFilePath);
        }
    }

    private async Task SaveToFile(string filePath)
    {
        try
        {
            var workflow = CreateWorkflowFromUI();
            workflow.ModifiedAt = DateTime.UtcNow;
            await _serializer.SaveAsync(workflow, filePath);
            StatusText = $"Saved: {System.IO.Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save workflow: {ex.Message}", "Save Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task LoadWorkflow()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Workflow (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var workflow = await _serializer.LoadAsync(dialog.FileName);
                LoadWorkflowToUI(workflow);
                _currentFilePath = dialog.FileName;
                _undoManager.Clear();
                StatusText = $"Loaded: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load workflow: {ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private Workflow CreateWorkflowFromUI()
    {
        var workflow = new Workflow
        {
            Name = WorkflowName,
            Actions = Actions.Select(vm => vm.ToAction()).ToList(),
            Triggers = Triggers.Select(vm => vm.ToTrigger()).ToList()
        };

        foreach (var v in Variables)
        {
            if (!string.IsNullOrWhiteSpace(v.Key))
            {
                workflow.Variables[v.Key] = v.Value;
            }
        }

        return workflow;
    }

    private void LoadWorkflowToUI(Workflow workflow)
    {
        Actions.Clear();
        Triggers.Clear();
        Variables.Clear();

        WorkflowName = workflow.Name;

        foreach (var action in workflow.Actions)
        {
            Actions.Add(ActionViewModel.FromAction(action));
        }

        foreach (var trigger in workflow.Triggers)
        {
            Triggers.Add(TriggerViewModel.FromTrigger(trigger));
        }

        foreach (var kvp in workflow.Variables)
        {
            Variables.Add(new VariableItemViewModel
            {
                Key = kvp.Key,
                Value = kvp.Value?.ToString() ?? ""
            });
        }
    }

    // Trigger Operations
    [RelayCommand]
    private void AddTimerTrigger()
    {
        var trigger = new TimerTriggerViewModel();
        _undoManager.Execute(new AddTriggerCommand(Triggers, trigger, Triggers.Count));
        SelectedTrigger = trigger;
    }

    [RelayCommand]
    private void AddFileSystemTrigger()
    {
        var trigger = new FileSystemTriggerViewModel();
        _undoManager.Execute(new AddTriggerCommand(Triggers, trigger, Triggers.Count));
        SelectedTrigger = trigger;
    }

    [RelayCommand]
    private void AddHotkeyTrigger()
    {
        var trigger = new HotkeyTriggerViewModel();
        _undoManager.Execute(new AddTriggerCommand(Triggers, trigger, Triggers.Count));
        SelectedTrigger = trigger;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveTrigger))]
    private void RemoveTrigger()
    {
        if (SelectedTrigger is null) return;

        var index = Triggers.IndexOf(SelectedTrigger);
        var trigger = SelectedTrigger;
        _undoManager.Execute(new RemoveTriggerCommand(Triggers, trigger, index));

        if (Triggers.Count > 0)
        {
            SelectedTrigger = Triggers[Math.Min(index, Triggers.Count - 1)];
        }
        else
        {
            SelectedTrigger = null;
        }
    }

    private bool CanRemoveTrigger() => SelectedTrigger is not null && !IsRunning;

    // Variable Operations
    [RelayCommand]
    private void AddVariable()
    {
        var variable = new VariableItemViewModel { Key = "newVariable", Value = "" };
        _undoManager.Execute(new AddVariableCommand(Variables, variable, Variables.Count));
        SelectedVariable = variable;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveVariable))]
    private void RemoveVariable()
    {
        if (SelectedVariable is null) return;

        var index = Variables.IndexOf(SelectedVariable);
        var variable = SelectedVariable;
        _undoManager.Execute(new RemoveVariableCommand(Variables, variable, index));

        if (Variables.Count > 0)
        {
            SelectedVariable = Variables[Math.Min(index, Variables.Count - 1)];
        }
        else
        {
            SelectedVariable = null;
        }
    }

    private bool CanRemoveVariable() => SelectedVariable is not null && !IsRunning;

    // Action Operations
    [RelayCommand]
    private void AddMouseClick()
    {
        var action = new MouseClickViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddKeyPress()
    {
        var action = new KeyPressViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddDelay()
    {
        var action = new DelayViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddSetVariable()
    {
        var action = new SetVariableViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddCondition()
    {
        var action = new ConditionViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddRepeat()
    {
        var action = new RepeatViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddWhile()
    {
        var action = new WhileViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddBreak()
    {
        var action = new BreakViewModel();
        AddAction(action);
    }

    [RelayCommand]
    private void AddContinue()
    {
        var action = new ContinueViewModel();
        AddAction(action);
    }

    private void AddAction(ActionViewModel action)
    {
        // If a container is selected, add to its children (not undoable at top level)
        if (SelectedAction is ContainerActionViewModel container)
        {
            container.Children.Add(action);
        }
        else if (SelectedAction is ConditionViewModel condition)
        {
            // Add to Then branch by default
            condition.ThenActions.Add(action);
        }
        else
        {
            _undoManager.Execute(new AddActionCommand(Actions, action, Actions.Count));
        }
        SelectedAction = action;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveAction))]
    private void RemoveAction()
    {
        if (SelectedAction is null) return;

        var index = Actions.IndexOf(SelectedAction);
        if (index < 0) return;

        var action = SelectedAction;
        _undoManager.Execute(new RemoveActionCommand(Actions, action, index));

        if (Actions.Count > 0)
        {
            SelectedAction = Actions[Math.Min(index, Actions.Count - 1)];
        }
        else
        {
            SelectedAction = null;
        }
    }

    private bool CanRemoveAction() => SelectedAction is not null && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedAction is null) return;

        var index = Actions.IndexOf(SelectedAction);
        if (index > 0)
        {
            _undoManager.Execute(new MoveActionCommand(Actions, index, index - 1));
        }
    }

    private bool CanMoveUp() => SelectedAction is not null
        && Actions.IndexOf(SelectedAction) > 0
        && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedAction is null) return;

        var index = Actions.IndexOf(SelectedAction);
        if (index < Actions.Count - 1)
        {
            _undoManager.Execute(new MoveActionCommand(Actions, index, index + 1));
        }
    }

    private bool CanMoveDown() => SelectedAction is not null
        && Actions.IndexOf(SelectedAction) < Actions.Count - 1
        && !IsRunning;

    /// <summary>
    /// Move an action by index. Used by drag-and-drop behavior.
    /// </summary>
    public void MoveActionByIndex(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Actions.Count) return;
        if (newIndex < 0 || newIndex >= Actions.Count) return;
        if (oldIndex == newIndex) return;

        _undoManager.Execute(new MoveActionCommand(Actions, oldIndex, newIndex));
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        var actions = Actions.Select(vm => vm.ToAction()).ToList();
        await _engine.StartAsync(actions);
    }

    private bool CanStart() => !IsRunning && Actions.Count > 0;

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _engine.Stop();
    }

    private bool CanStop() => IsRunning;

    // Execution History Commands
    [RelayCommand]
    private void ClearHistory()
    {
        ExecutionHistory.Clear();
        SelectedExecution = null;
    }

    [RelayCommand]
    private void ToggleLogPanel()
    {
        IsLogPanelVisible = !IsLogPanelVisible;
    }

    // PowerShell Export
    [RelayCommand]
    private void ExportAsPowerShell()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PowerShell Script (*.ps1)|*.ps1|All Files (*.*)|*.*",
            DefaultExt = ".ps1",
            FileName = WorkflowName
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var workflow = CreateWorkflowFromUI();
                var exporter = new PowerShellExporter();
                var script = exporter.Export(workflow);
                System.IO.File.WriteAllText(dialog.FileName, script);
                StatusText = $"Exported: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Hotkey capture commands
    [RelayCommand(CanExecute = nameof(CanSetHotkey))]
    private void SetStartHotkey()
    {
        StartHotkeyCapture("Press a key for Start hotkey...", hotkey => StartHotkey = hotkey);
    }

    [RelayCommand(CanExecute = nameof(CanSetHotkey))]
    private void SetStopHotkey()
    {
        StartHotkeyCapture("Press a key for Stop hotkey...", hotkey => StopHotkey = hotkey);
    }

    private bool CanSetHotkey() => !IsCapturingHotkey;

    private Action<HotkeyBinding>? _hotkeyCallback;

    private void StartHotkeyCapture(string label, Action<HotkeyBinding> callback)
    {
        IsCapturingHotkey = true;
        CapturingHotkeyLabel = label;
        _hotkeyCallback = callback;

        // Temporarily stop the main hotkey service so it doesn't interfere
        _hotkeyService.Stop();

        _hotkeyCapture = new GlobalKeyboardHook();
        _hotkeyCapture.KeyDown += OnHotkeyCaptureKeyDown;
        _hotkeyCapture.Start();
    }

    private void OnHotkeyCaptureKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        // Ignore modifier-only keys
        if (e.VirtualKeyCode is 0x10 or 0x11 or 0x12)
            return;

        // Escape cancels
        if (e.VirtualKeyCode == 0x1B)
        {
            StopHotkeyCapture();
            return;
        }

        var hotkey = new HotkeyBinding(e.VirtualKeyCode, e.Ctrl, e.Alt, e.Shift);
        _hotkeyCallback?.Invoke(hotkey);
        StopHotkeyCapture();
    }

    private void StopHotkeyCapture()
    {
        _hotkeyCapture?.Stop();
        _hotkeyCapture?.Dispose();
        _hotkeyCapture = null;
        _hotkeyCallback = null;
        IsCapturingHotkey = false;
        CapturingHotkeyLabel = "";

        // Restart the main hotkey service
        _hotkeyService.Start();
    }

    [RelayCommand]
    private void ClearStartHotkey()
    {
        StartHotkey = HotkeyBinding.Empty;
    }

    [RelayCommand]
    private void ClearStopHotkey()
    {
        StopHotkey = HotkeyBinding.Empty;
    }
}
