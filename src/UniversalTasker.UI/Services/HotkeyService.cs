using UniversalTasker.Core.Input;

namespace UniversalTasker.UI.Services;

public record HotkeyBinding(int VirtualKeyCode, bool Ctrl, bool Alt, bool Shift)
{
    public string DisplayText => VirtualKeyCode > 0
        ? VirtualKeyHelper.GetKeyDisplayString(VirtualKeyCode, Ctrl, Alt, Shift)
        : "(None)";

    public bool Matches(KeyboardHookEventArgs e) =>
        VirtualKeyCode == e.VirtualKeyCode &&
        Ctrl == e.Ctrl &&
        Alt == e.Alt &&
        Shift == e.Shift;

    public static HotkeyBinding Empty => new(0, false, false, false);
}

public class HotkeyService : IDisposable
{
    private GlobalKeyboardHook? _hook;
    private bool _disposed;

    public HotkeyBinding StartHotkey { get; set; } = new(0x75, false, false, false); // F6
    public HotkeyBinding StopHotkey { get; set; } = new(0x76, false, false, false);  // F7
    public bool UseSameHotkey { get; set; } = false;

    public event EventHandler? StartTriggered;
    public event EventHandler? StopTriggered;

    public void Start()
    {
        if (_hook is not null) return;

        _hook = new GlobalKeyboardHook();
        _hook.KeyDown += OnKeyDown;
        _hook.Start();
    }

    public void Stop()
    {
        _hook?.Stop();
        _hook?.Dispose();
        _hook = null;
    }

    private void OnKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        // Ignore modifier-only keys
        if (e.VirtualKeyCode is 0x10 or 0x11 or 0x12)
            return;

        if (UseSameHotkey)
        {
            if (StartHotkey.Matches(e))
            {
                StartTriggered?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            if (StartHotkey.Matches(e))
            {
                StartTriggered?.Invoke(this, EventArgs.Empty);
            }
            else if (StopHotkey.Matches(e))
            {
                StopTriggered?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
