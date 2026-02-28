using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Input;

namespace UniversalTasker.UI.ViewModels;

public partial class KeyPressViewModel : ActionViewModel
{
    public override string Category => "Input";
    public override string DisplayName => "Key Press";

    [ObservableProperty]
    private ushort _virtualKeyCode;

    [ObservableProperty]
    private bool _ctrl;

    [ObservableProperty]
    private bool _alt;

    [ObservableProperty]
    private bool _shift;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CaptureKeyCommand))]
    private bool _isCapturing;

    private GlobalKeyboardHook? _keyboardHook;

    public KeyPressViewModel()
    {
        Name = "Key Press";
    }

    public string KeyDisplayText => VirtualKeyCode > 0
        ? VirtualKeyHelper.GetKeyDisplayString(VirtualKeyCode, Ctrl, Alt, Shift)
        : "(None)";

    partial void OnVirtualKeyCodeChanged(ushort value) => OnPropertyChanged(nameof(KeyDisplayText));
    partial void OnCtrlChanged(bool value) => OnPropertyChanged(nameof(KeyDisplayText));
    partial void OnAltChanged(bool value) => OnPropertyChanged(nameof(KeyDisplayText));
    partial void OnShiftChanged(bool value) => OnPropertyChanged(nameof(KeyDisplayText));

    [RelayCommand(CanExecute = nameof(CanCaptureKey))]
    private void CaptureKey()
    {
        if (IsCapturing)
        {
            StopCapture();
            return;
        }

        IsCapturing = true;
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnKeyDown;
        _keyboardHook.Start();
    }

    private bool CanCaptureKey() => true;

    private void OnKeyDown(object? sender, KeyboardHookEventArgs e)
    {
        // Ignore modifier-only key presses
        if (e.VirtualKeyCode is 0x10 or 0x11 or 0x12) // Shift, Ctrl, Alt
            return;

        VirtualKeyCode = (ushort)e.VirtualKeyCode;
        Ctrl = e.Ctrl;
        Alt = e.Alt;
        Shift = e.Shift;

        StopCapture();
    }

    private void StopCapture()
    {
        _keyboardHook?.Stop();
        _keyboardHook?.Dispose();
        _keyboardHook = null;
        IsCapturing = false;
    }

    public override IAction ToAction()
    {
        return new KeyPressAction
        {
            Name = Name,
            VirtualKeyCode = VirtualKeyCode,
            Ctrl = Ctrl,
            Alt = Alt,
            Shift = Shift
        };
    }
}
