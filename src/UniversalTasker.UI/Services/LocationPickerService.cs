using System.Windows;
using UniversalTasker.Core.Input;

namespace UniversalTasker.UI.Services;

public class LocationPickedEventArgs : EventArgs
{
    public int X { get; }
    public int Y { get; }

    public LocationPickedEventArgs(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class LocationPickerService : IDisposable
{
    private GlobalMouseHook? _mouseHook;
    private Window? _overlayWindow;
    private TaskCompletionSource<(int X, int Y)>? _pickTcs;

    public async Task<(int X, int Y)?> PickLocationAsync()
    {
        _pickTcs = new TaskCompletionSource<(int X, int Y)>();

        _mouseHook = new GlobalMouseHook();
        _mouseHook.MouseDown += OnMouseDown;

        // Create a transparent overlay to show picking mode
        _overlayWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            Topmost = true,
            Left = 0,
            Top = 0,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight,
            Cursor = System.Windows.Input.Cursors.Cross,
            ShowInTaskbar = false
        };

        // Add a semi-transparent border to indicate pick mode
        _overlayWindow.Content = new System.Windows.Controls.Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(128, 0, 120, 215)),
            BorderThickness = new Thickness(4),
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(1, 0, 0, 0)) // Nearly transparent but clickable
        };

        _overlayWindow.Show();
        _mouseHook.Start();

        try
        {
            var result = await _pickTcs.Task;
            return result;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        finally
        {
            Cleanup();
        }
    }

    private void OnMouseDown(object? sender, MouseHookEventArgs e)
    {
        if (e.Button == MouseButton.Left)
        {
            _pickTcs?.TrySetResult((e.X, e.Y));
        }
        else if (e.Button == MouseButton.Right)
        {
            _pickTcs?.TrySetCanceled();
        }
    }

    public void Cancel()
    {
        _pickTcs?.TrySetCanceled();
    }

    private void Cleanup()
    {
        _mouseHook?.Stop();
        _mouseHook?.Dispose();
        _mouseHook = null;

        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    public void Dispose()
    {
        Cancel();
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
