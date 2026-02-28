using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("condition", "If/Else", "Flow")]
public class ConditionAction : ActionBase, IContainerAction
{
    public override string Category => "Flow";

    public Condition Condition { get; set; } = new();
    public List<IAction> ThenActions { get; } = new();
    public List<IAction> ElseActions { get; } = new();

    public ConditionAction()
    {
        Name = "If/Else";
    }

    public IEnumerable<IAction> GetChildActions()
    {
        foreach (var action in ThenActions)
            yield return action;
        foreach (var action in ElseActions)
            yield return action;
    }

    public override async Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        var evaluator = context.GetRequiredService<IExpressionEvaluator>();
        var result = evaluator.EvaluateCondition(Condition, context);

        context.Logger.LogInformation("Condition evaluated to {Result}", result);

        var actionsToRun = result ? ThenActions : ElseActions;

        foreach (var action in actionsToRun)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await action.ExecuteAsync(context);
        }
    }
}
