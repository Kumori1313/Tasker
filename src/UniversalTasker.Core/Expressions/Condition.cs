namespace UniversalTasker.Core.Expressions;

public class Condition
{
    public string LeftOperand { get; set; } = "";
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equals;
    public string RightOperand { get; set; } = "";

    public LogicalOperator? LogicalOp { get; set; }
    public Condition? NextCondition { get; set; }
}
