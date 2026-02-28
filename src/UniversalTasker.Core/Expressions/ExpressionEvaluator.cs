using System.Globalization;
using System.Text.RegularExpressions;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.Core.Expressions;

public partial class ExpressionEvaluator : IExpressionEvaluator
{
    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex VariablePattern();

    public object? Evaluate(string expression, Actions.ExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var trimmed = expression.Trim();

        // Check if it's a simple variable reference: {varName}
        var match = VariablePattern().Match(trimmed);
        if (match.Success && match.Value == trimmed)
        {
            var varName = match.Groups[1].Value;
            return ResolveVariable(varName, context);
        }

        // Otherwise, interpolate and return as string
        return Interpolate(expression, context);
    }

    public string Interpolate(string template, Actions.ExecutionContext context)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return VariablePattern().Replace(template, match =>
        {
            var varName = match.Groups[1].Value;
            var value = ResolveVariable(varName, context);
            return value?.ToString() ?? "";
        });
    }

    public bool EvaluateCondition(Condition condition, Actions.ExecutionContext context)
    {
        var result = EvaluateSingleCondition(condition, context);

        if (condition.NextCondition != null && condition.LogicalOp.HasValue)
        {
            var nextResult = EvaluateCondition(condition.NextCondition, context);
            result = condition.LogicalOp.Value switch
            {
                LogicalOperator.And => result && nextResult,
                LogicalOperator.Or => result || nextResult,
                _ => result
            };
        }

        return result;
    }

    private bool EvaluateSingleCondition(Condition condition, Actions.ExecutionContext context)
    {
        var left = ResolveOperand(condition.LeftOperand, context);
        var right = ResolveOperand(condition.RightOperand, context);

        return condition.Operator switch
        {
            ComparisonOperator.Equals => AreEqual(left, right),
            ComparisonOperator.NotEquals => !AreEqual(left, right),
            ComparisonOperator.LessThan => Compare(left, right) < 0,
            ComparisonOperator.GreaterThan => Compare(left, right) > 0,
            ComparisonOperator.LessOrEqual => Compare(left, right) <= 0,
            ComparisonOperator.GreaterOrEqual => Compare(left, right) >= 0,
            ComparisonOperator.Contains => StringContains(left, right),
            ComparisonOperator.StartsWith => StringStartsWith(left, right),
            ComparisonOperator.EndsWith => StringEndsWith(left, right),
            _ => false
        };
    }

    private object? ResolveOperand(string operand, Actions.ExecutionContext context)
    {
        if (string.IsNullOrEmpty(operand))
            return null;

        var trimmed = operand.Trim();

        // Check if it's a variable reference: {varName}
        var match = VariablePattern().Match(trimmed);
        if (match.Success && match.Value == trimmed)
        {
            return ResolveVariable(match.Groups[1].Value, context);
        }

        // Check if it's a quoted string
        if ((trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
            (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
        {
            return trimmed[1..^1];
        }

        // Try to parse as number
        if (double.TryParse(trimmed, CultureInfo.InvariantCulture, out var num))
        {
            return num;
        }

        // Try boolean
        if (bool.TryParse(trimmed, out var boolVal))
        {
            return boolVal;
        }

        // Return as string literal
        return trimmed;
    }

    private object? ResolveVariable(string name, Actions.ExecutionContext context)
    {
        if (context.Variables.TryGetValue(name, out var value))
        {
            return value;
        }

        // Built-in variables
        return name.ToLowerInvariant() switch
        {
            "timestamp" => DateTime.Now,
            "now" => DateTime.Now,
            "date" => DateTime.Now.Date,
            "time" => DateTime.Now.TimeOfDay,
            "mousex" => Input.InputSimulator.GetMousePosition().X,
            "mousey" => Input.InputSimulator.GetMousePosition().Y,
            _ => null
        };
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        // Try numeric comparison
        if (TryGetNumber(left, out var leftNum) && TryGetNumber(right, out var rightNum))
        {
            return Math.Abs(leftNum - rightNum) < 0.0001;
        }

        // String comparison (case-insensitive)
        return string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static int Compare(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        // Try numeric comparison
        if (TryGetNumber(left, out var leftNum) && TryGetNumber(right, out var rightNum))
        {
            return leftNum.CompareTo(rightNum);
        }

        // String comparison
        return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool StringContains(object? left, object? right)
    {
        var leftStr = left?.ToString() ?? "";
        var rightStr = right?.ToString() ?? "";
        return leftStr.Contains(rightStr, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StringStartsWith(object? left, object? right)
    {
        var leftStr = left?.ToString() ?? "";
        var rightStr = right?.ToString() ?? "";
        return leftStr.StartsWith(rightStr, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StringEndsWith(object? left, object? right)
    {
        var leftStr = left?.ToString() ?? "";
        var rightStr = right?.ToString() ?? "";
        return leftStr.EndsWith(rightStr, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetNumber(object? value, out double number)
    {
        number = 0;
        if (value == null) return false;

        if (value is double d) { number = d; return true; }
        if (value is int i) { number = i; return true; }
        if (value is long l) { number = l; return true; }
        if (value is float f) { number = f; return true; }
        if (value is decimal dec) { number = (double)dec; return true; }

        return double.TryParse(value.ToString(), CultureInfo.InvariantCulture, out number);
    }
}
