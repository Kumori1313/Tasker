using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.Core.Actions;

[ActionMetadata("setvariable", "Set Variable", "Variables")]
public class SetVariableAction : ActionBase
{
    public override string Category => "Variables";

    public string VariableName { get; set; } = "";
    public string Value { get; set; } = "";
    public bool EvaluateAsExpression { get; set; }

    public SetVariableAction()
    {
        Name = "Set Variable";
    }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);

        var evaluator = context.GetRequiredService<IExpressionEvaluator>();

        object? resolvedValue;
        if (EvaluateAsExpression)
        {
            resolvedValue = evaluator.Evaluate(Value, context);
        }
        else
        {
            resolvedValue = evaluator.Interpolate(Value, context);
        }

        context.Variables[VariableName] = resolvedValue;

        context.Logger.LogInformation("Set variable {Name} = {Value}", VariableName, resolvedValue);

        return Task.CompletedTask;
    }
}
