using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.Tests.Expressions;

public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    private ExecutionContext CreateContext(Dictionary<string, object?>? vars = null)
    {
        return new ExecutionContext(variables: vars);
    }

    [Fact]
    public void Interpolate_ReplacesVariables()
    {
        var ctx = CreateContext(new() { ["name"] = "World" });
        var result = _evaluator.Interpolate("Hello, {name}!", ctx);

        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Interpolate_MultipleVariables()
    {
        var ctx = CreateContext(new() { ["first"] = "John", ["last"] = "Doe" });
        var result = _evaluator.Interpolate("{first} {last}", ctx);

        Assert.Equal("John Doe", result);
    }

    [Fact]
    public void Interpolate_MissingVariable_ReplacesWithEmpty()
    {
        var ctx = CreateContext();
        var result = _evaluator.Interpolate("Hi {missing}!", ctx);

        Assert.Equal("Hi !", result);
    }

    [Fact]
    public void Interpolate_EmptyTemplate_ReturnsEmpty()
    {
        var ctx = CreateContext();
        Assert.Equal("", _evaluator.Interpolate("", ctx));
    }

    [Fact]
    public void Evaluate_SimpleVariableReference_ReturnsValue()
    {
        var ctx = CreateContext(new() { ["x"] = 42 });
        var result = _evaluator.Evaluate("{x}", ctx);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Evaluate_EmptyExpression_ReturnsNull()
    {
        var ctx = CreateContext();
        Assert.Null(_evaluator.Evaluate("", ctx));
    }

    [Fact]
    public void EvaluateCondition_Equals_True()
    {
        var ctx = CreateContext(new() { ["x"] = 10 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.Equals,
            RightOperand = "10"
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_Equals_False()
    {
        var ctx = CreateContext(new() { ["x"] = 10 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.Equals,
            RightOperand = "20"
        };

        Assert.False(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_NotEquals()
    {
        var ctx = CreateContext(new() { ["x"] = 5 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.NotEquals,
            RightOperand = "10"
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_GreaterThan()
    {
        var ctx = CreateContext(new() { ["x"] = 10 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.GreaterThan,
            RightOperand = "5"
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_LessThan()
    {
        var ctx = CreateContext(new() { ["x"] = 3 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.LessThan,
            RightOperand = "5"
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_Contains()
    {
        var ctx = CreateContext(new() { ["msg"] = "Hello World" });
        var condition = new Condition
        {
            LeftOperand = "{msg}",
            Operator = ComparisonOperator.Contains,
            RightOperand = "\"World\""
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_StartsWith()
    {
        var ctx = CreateContext(new() { ["msg"] = "Hello World" });
        var condition = new Condition
        {
            LeftOperand = "{msg}",
            Operator = ComparisonOperator.StartsWith,
            RightOperand = "\"Hello\""
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_ChainedCondition_And()
    {
        var ctx = CreateContext(new() { ["x"] = 10, ["y"] = 20 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.Equals,
            RightOperand = "10",
            LogicalOp = LogicalOperator.And,
            NextCondition = new Condition
            {
                LeftOperand = "{y}",
                Operator = ComparisonOperator.Equals,
                RightOperand = "20"
            }
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_ChainedCondition_Or()
    {
        var ctx = CreateContext(new() { ["x"] = 10 });
        var condition = new Condition
        {
            LeftOperand = "{x}",
            Operator = ComparisonOperator.Equals,
            RightOperand = "99",
            LogicalOp = LogicalOperator.Or,
            NextCondition = new Condition
            {
                LeftOperand = "{x}",
                Operator = ComparisonOperator.Equals,
                RightOperand = "10"
            }
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }

    [Fact]
    public void EvaluateCondition_StringComparison_CaseInsensitive()
    {
        var ctx = CreateContext(new() { ["name"] = "Alice" });
        var condition = new Condition
        {
            LeftOperand = "{name}",
            Operator = ComparisonOperator.Equals,
            RightOperand = "\"alice\""
        };

        Assert.True(_evaluator.EvaluateCondition(condition, ctx));
    }
}
