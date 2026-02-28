using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Input;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("mouseclick", "Mouse Click", "Input")]
public class MouseClickAction : ActionBase
{
    public override string Category => "Input";

    public MouseButton Button { get; set; } = MouseButton.Left;
    public int X { get; set; }
    public int Y { get; set; }
    public int ClickCount { get; set; } = 1;

    public MouseClickAction()
    {
        Name = "Mouse Click";
    }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        context.Logger.LogInformation(
            "Executing mouse {Button} click at ({X}, {Y}) x{Count}",
            Button, X, Y, ClickCount);

        InputSimulator.MouseClick(Button, X, Y, ClickCount);

        return Task.CompletedTask;
    }
}
