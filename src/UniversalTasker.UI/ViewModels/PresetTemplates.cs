using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Input;
using UniversalTasker.Core.Triggers;
using UniversalTasker.Core.Workflows;

namespace UniversalTasker.UI.ViewModels;

/// <summary>
/// Pre-built workflow templates that users can load as starting points.
/// </summary>
public static class PresetTemplates
{
    /// <summary>
    /// Autoclicker: repeats a left-click + short delay 100 times.
    /// Users should set X/Y to their target coordinates in the Mouse Click action.
    /// </summary>
    public static Workflow Autoclicker()
    {
        var click = new MouseClickAction
        {
            Name = "Click",
            Button = MouseButton.Left,
            X = 0,
            Y = 0,
            ClickCount = 1
        };

        var delay = new DelayAction
        {
            Name = "Delay",
            DurationMs = 50
        };

        var repeat = new RepeatAction
        {
            Name = "Repeat 100×",
            RepeatCount = 100,
            CounterVariable = "i"
        };
        repeat.Actions.Add(click);
        repeat.Actions.Add(delay);

        return new Workflow
        {
            Name = "Autoclicker",
            Description = "Repeatedly clicks the mouse. Set X and Y in the Mouse Click action.",
            Actions = new List<IAction> { repeat }
        };
    }

    /// <summary>
    /// Typing macro skeleton: delays for focus, then types two keystrokes via a Sequence.
    /// Add or replace Key Press actions inside the Sequence as needed.
    /// </summary>
    public static Workflow TypingMacro()
    {
        var seq = new SequenceAction { Name = "Type Keys" };
        seq.Actions.Add(new KeyPressAction { Name = "Key H", VirtualKeyCode = 0x48 });
        seq.Actions.Add(new DelayAction    { Name = "Inter-key Delay", DurationMs = 100 });
        seq.Actions.Add(new KeyPressAction { Name = "Key I", VirtualKeyCode = 0x49 });

        return new Workflow
        {
            Name = "Typing Macro",
            Description = "Types keystrokes in sequence. Add Key Press actions inside the Sequence.",
            Actions = new List<IAction>
            {
                new DelayAction { Name = "Focus Delay", DurationMs = 500 },
                seq
            }
        };
    }

    /// <summary>
    /// File monitor skeleton: watches the Documents folder for new files.
    /// Set the Path in the trigger, then add actions inside the Sequence.
    /// </summary>
    public static Workflow FileMonitor()
    {
        var seq = new SequenceAction { Name = "On File Created" };
        seq.Actions.Add(new DelayAction { Name = "Processing Delay", DurationMs = 200 });

        var trigger = new FileSystemTrigger
        {
            Name = "Watch Folder",
            Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Filter = "*.*",
            IncludeSubdirectories = false,
            WatchCreated = true,
            WatchChanged = false,
            WatchDeleted = false,
            WatchRenamed = false
        };

        return new Workflow
        {
            Name = "File Monitor",
            Description = "Watches a folder for new files. Set the Path in the File System trigger.",
            Triggers = new List<ITrigger> { trigger },
            Actions = new List<IAction> { seq }
        };
    }
}
