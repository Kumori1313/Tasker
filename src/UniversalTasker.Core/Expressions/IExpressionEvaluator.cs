using UniversalTasker.Core.Actions;

namespace UniversalTasker.Core.Expressions;

public interface IExpressionEvaluator
{
    object? Evaluate(string expression, Actions.ExecutionContext context);
    string Interpolate(string template, Actions.ExecutionContext context);
    bool EvaluateCondition(Condition condition, Actions.ExecutionContext context);
}
