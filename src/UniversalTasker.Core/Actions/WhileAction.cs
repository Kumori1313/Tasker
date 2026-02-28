using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("while", "While Loop", "Flow")]
public class WhileAction : ActionBase, IContainerAction
{
    public override string Category => "Flow";

    public Condition Condition { get; set; } = new();
    public int MaxIterations { get; set; } = 10000;
    public List<IAction> Actions { get; } = new();

    public WhileAction()
    {
        Name = "While Loop";
    }

    public IEnumerable<IAction> GetChildActions() => Actions;

    public override async Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        int iterations = 0;

        context.Logger.LogInformation("Starting while loop");

        while (evaluator.EvaluateCondition(Condition, context))
        {
            if (++iterations > MaxIterations)
            {
                context.Logger.LogWarning("While loop exceeded maximum iterations ({Max})", MaxIterations);
                throw new InvalidOperationException($"While loop exceeded maximum iterations ({MaxIterations})");
            }

            context.CancellationToken.ThrowIfCancellationRequested();

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
                context.Logger.LogInformation("Break encountered, exiting while loop");
                break;
            }
            catch (LoopContinueException)
            {
                context.Logger.LogInformation("Continue encountered");
                continue;
            }
        }

        context.Logger.LogInformation("While loop completed after {Count} iterations", iterations);
    }
}
