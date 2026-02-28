using Microsoft.Extensions.Logging;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("repeat", "Repeat", "Flow")]
public class RepeatAction : ActionBase, IContainerAction
{
    public override string Category => "Flow";

    public int RepeatCount { get; set; } = 1;
    public string CounterVariable { get; set; } = "i";
    public List<IAction> Actions { get; } = new();

    public RepeatAction()
    {
        Name = "Repeat";
    }

    public IEnumerable<IAction> GetChildActions() => Actions;

    public override async Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        context.Logger.LogInformation("Starting repeat loop for {Count} iterations", RepeatCount);

        for (int i = 0; i < RepeatCount; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.Variables[CounterVariable] = i;

            try
            {
                foreach (var action in Actions)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    await action.ExecuteAsync(context);
                }
            }
            catch (LoopBreakException)
            {
                context.Logger.LogInformation("Break encountered at iteration {Index}, exiting loop", i);
                break;
            }
            catch (LoopContinueException)
            {
                context.Logger.LogInformation("Continue encountered at iteration {Index}", i);
                continue;
            }
        }

        context.Logger.LogInformation("Repeat loop completed");
    }
}
