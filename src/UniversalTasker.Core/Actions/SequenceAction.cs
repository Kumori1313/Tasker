using Microsoft.Extensions.Logging;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("sequence", "Sequence", "Flow")]
public class SequenceAction : ActionBase
{
    public override string Category => "Flow";

    public List<IAction> Actions { get; } = new();

    public SequenceAction()
    {
        Name = "Sequence";
    }

    public override async Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        context.Logger.LogInformation("Starting sequence with {Count} actions", Actions.Count);

        foreach (var action in Actions)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await action.ExecuteAsync(context);
        }

        context.Logger.LogInformation("Sequence completed");
    }
}
