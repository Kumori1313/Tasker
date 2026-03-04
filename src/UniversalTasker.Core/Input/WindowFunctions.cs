using System.Runtime.InteropServices;

namespace UniversalTasker.Core.Input;

public static class WindowFunctions
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    /// <summary>
    /// Returns true if a top-level window with the exact title exists.
    /// </summary>
    public static bool WindowExists(string title)
        => FindWindow(null, title) != IntPtr.Zero;

    /// <summary>
    /// Returns the color of the screen pixel at (x, y) as a "#RRGGBB" hex string,
    /// or null if the coordinates are out of bounds or the call fails.
    /// </summary>
    public static string? GetPixelColor(int x, int y)
    {
        var hdc = GetDC(IntPtr.Zero);
        try
        {
            var colorRef = GetPixel(hdc, x, y);
            // GetPixel returns CLR_INVALID (0xFFFFFFFF) on error or out-of-bounds coordinates
            if (colorRef == 0xFFFFFFFF)
                return null;

            // COLORREF encoding: 0x00BBGGRR
            var r = (byte)(colorRef & 0xFF);
            var g = (byte)((colorRef >> 8) & 0xFF);
            var b = (byte)((colorRef >> 16) & 0xFF);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }
}
