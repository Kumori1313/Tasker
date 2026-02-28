using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Input;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("keypress", "Key Press", "Input")]
public class KeyPressAction : ActionBase
{
    public override string Category => "Input";

    public ushort VirtualKeyCode { get; set; }
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    public KeyPressAction()
    {
        Name = "Key Press";
    }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        var modifiers = new List<string>();
        if (Ctrl) modifiers.Add("Ctrl");
        if (Alt) modifiers.Add("Alt");
        if (Shift) modifiers.Add("Shift");

        var modifierString = modifiers.Count > 0 ? string.Join("+", modifiers) + "+" : "";

        context.Logger.LogInformation(
            "Executing key press: {Modifiers}0x{KeyCode:X2}",
            modifierString, VirtualKeyCode);

        InputSimulator.KeyPress(VirtualKeyCode, Ctrl, Alt, Shift);

        return Task.CompletedTask;
    }
}
