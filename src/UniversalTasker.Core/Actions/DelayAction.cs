using Microsoft.Extensions.Logging;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("delay", "Delay", "Flow")]
public class DelayAction : ActionBase
{
    public override string Category => "Flow";

    public int DurationMs { get; set; } = 1000;

    public DelayAction()
    {
        Name = "Delay";
    }

    public override async Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        context.Logger.LogInformation("Waiting for {Duration}ms", DurationMs);

        await Task.Delay(DurationMs, context.CancellationToken);
    }
}
