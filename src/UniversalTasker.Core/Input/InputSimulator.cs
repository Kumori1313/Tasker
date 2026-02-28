using System.Runtime.InteropServices;

namespace UniversalTasker.Core.Input;

public static class InputSimulator
{
    public static void MoveMouse(int x, int y)
    {
        NativeMethods.SetCursorPos(x, y);
    }

    public static (int X, int Y) GetMousePosition()
    {
        NativeMethods.GetCursorPos(out var point);
        return (point.X, point.Y);
    }

    public static void MouseClick(MouseButton button, int? x = null, int? y = null, int clickCount = 1)
    {
        if (x.HasValue && y.HasValue)
        {
            MoveMouse(x.Value, y.Value);
        }

        var (downFlag, upFlag) = button switch
        {
            MouseButton.Left => (NativeMethods.MOUSEEVENTF_LEFTDOWN, NativeMethods.MOUSEEVENTF_LEFTUP),
            MouseButton.Right => (NativeMethods.MOUSEEVENTF_RIGHTDOWN, NativeMethods.MOUSEEVENTF_RIGHTUP),
            MouseButton.Middle => (NativeMethods.MOUSEEVENTF_MIDDLEDOWN, NativeMethods.MOUSEEVENTF_MIDDLEUP),
            _ => throw new ArgumentOutOfRangeException(nameof(button))
        };

        for (int i = 0; i < clickCount; i++)
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].u.mi.dwFlags = downFlag;

            inputs[1].type = NativeMethods.INPUT_MOUSE;
            inputs[1].u.mi.dwFlags = upFlag;

            NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        }
    }

    public static void KeyPress(ushort virtualKeyCode, bool ctrl = false, bool alt = false, bool shift = false)
    {
        var modifiersDown = new List<NativeMethods.INPUT>();
        var modifiersUp = new List<NativeMethods.INPUT>();

        if (ctrl) AddModifier(modifiersDown, modifiersUp, 0x11); // VK_CONTROL
        if (alt) AddModifier(modifiersDown, modifiersUp, 0x12);  // VK_MENU (Alt)
        if (shift) AddModifier(modifiersDown, modifiersUp, 0x10); // VK_SHIFT

        var keyDown = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    dwFlags = NativeMethods.KEYEVENTF_KEYDOWN
                }
            }
        };

        var keyUp = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP
                }
            }
        };

        var allInputs = new List<NativeMethods.INPUT>();
        allInputs.AddRange(modifiersDown);
        allInputs.Add(keyDown);
        allInputs.Add(keyUp);
        modifiersUp.Reverse();
        allInputs.AddRange(modifiersUp);

        NativeMethods.SendInput((uint)allInputs.Count, allInputs.ToArray(), Marshal.SizeOf<NativeMethods.INPUT>());
    }

    public static void KeyDown(ushort virtualKeyCode)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    dwFlags = NativeMethods.KEYEVENTF_KEYDOWN
                }
            }
        };

        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());
    }

    public static void KeyUp(ushort virtualKeyCode)
    {
        var input = new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = virtualKeyCode,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP
                }
            }
        };

        NativeMethods.SendInput(1, [input], Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void AddModifier(List<NativeMethods.INPUT> down, List<NativeMethods.INPUT> up, ushort vk)
    {
        down.Add(new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = NativeMethods.KEYEVENTF_KEYDOWN
                }
            }
        });

        up.Add(new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = NativeMethods.KEYEVENTF_KEYUP
                }
            }
        });
    }
}
