using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Input;

namespace UniversalTasker.Core.Triggers;

[TriggerMetadata("hotkey", "Hotkey", "Fires when a keyboard shortcut is pressed")]
public class HotkeyTrigger : TriggerBase
{
    private GlobalKeyboardHook? _hook;

    /// <summary>
    /// Virtual key code for the trigger key.
    /// </summary>
    public int VirtualKeyCode { get; set; }

    /// <summary>
    /// Whether Ctrl must be held.
    /// </summary>
    public bool Ctrl { get; set; }

    /// <summary>
    /// Whether Alt must be held.
    /// </summary>
    public bool Alt { get; set; }

    /// <summary>
    /// Whether Shift must be held.
    /// </summary>
    public bool Shift { get; set; }

    public HotkeyTrigger() : this(null)
    {
    }

    public HotkeyTrigger(ILogger? logger) : base(logger)
    {
        Name = "Hotkey Trigger";
    }

    public string GetDisplayText()
    {
        if (VirtualKeyCode == 0) return "(None)";
        return VirtualKeyHelper.GetKeyDisplayString(VirtualKeyCode, Ctrl, Alt, Shift);
    }

    public void SetFromKeyboardEvent(KeyboardHookEventArgs e)
    {
        VirtualKeyCode = e.VirtualKeyCode;
        Ctrl = e.Ctrl;
        Alt = e.Alt;
        Shift = e.Shift;
    }

    public bool Matches(KeyboardHookEventArgs e)
    {
        return VirtualKeyCode == e.VirtualKeyCode &&
               Ctrl == e.Ctrl &&
               Alt == e.Alt &&
               Shift == e.Shift;
    }

    protected override void OnStart()
    {
        if (VirtualKeyCode == 0)
        {
            Logger.LogWarning("Hotkey trigger has no key configured, not starting");
            return;
        }

        _hook = new GlobalKeyboardHook();
        _hook.KeyDown += OnKeyDown;
        _hook.Start();

        Logger.LogInformation("Hotkey trigger listening for: {Hotkey}", GetDisplayText());
    }

    protected override void OnStop()
    {
        if (_hook != null)
        {
            _hook.KeyDown -= OnKeyDown;
            _hook.Stop();
            _hook.Dispose();
            _hook = null;
        }
    }

    private void OnKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        // Ignore modifier-only keys
        if (e.VirtualKeyCode is 0x10 or 0x11 or 0x12) return;

        if (Matches(e))
        {
            var data = new Dictionary<string, object?>
            {
                ["virtualKeyCode"] = e.VirtualKeyCode,
                ["ctrl"] = e.Ctrl,
                ["alt"] = e.Alt,
                ["shift"] = e.Shift,
                ["displayText"] = GetDisplayText(),
                ["timestamp"] = DateTime.Now
            };

            RaiseFired(data);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook?.Dispose();
            _hook = null;
        }
        base.Dispose(disposing);
    }
}
