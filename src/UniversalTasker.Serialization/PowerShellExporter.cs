using System.Text;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;
using UniversalTasker.Core.Input;
using UniversalTasker.Core.Workflows;

namespace UniversalTasker.Serialization;

public class PowerShellExporter
{
    public string Export(Workflow workflow)
    {
        var sb = new StringBuilder();

        EmitHeader(sb, workflow);
        EmitPInvokeSignatures(sb);
        EmitVariables(sb, workflow);
        EmitActions(sb, workflow.Actions, indent: 0);

        return sb.ToString();
    }

    private void EmitHeader(StringBuilder sb, Workflow workflow)
    {
        sb.AppendLine("# PowerShell automation script");
        sb.AppendLine($"# Generated from workflow: {EscapeComment(workflow.Name)}");
        sb.AppendLine($"# Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        if (!string.IsNullOrEmpty(workflow.Description))
        {
            sb.AppendLine($"# Description: {EscapeComment(workflow.Description)}");
        }
        sb.AppendLine();
    }

    private void EmitPInvokeSignatures(StringBuilder sb)
    {
        sb.AppendLine(@"Add-Type @""
using System;
using System.Runtime.InteropServices;

public class NativeInput
{
    [DllImport(""user32.dll"")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport(""user32.dll"")]
    public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, IntPtr dwExtraInfo);

    [DllImport(""user32.dll"")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint KEYEVENTF_KEYUP = 0x0002;
}
""@");
        sb.AppendLine();
    }

    private void EmitVariables(StringBuilder sb, Workflow workflow)
    {
        if (workflow.Variables.Count == 0) return;

        sb.AppendLine("# Variables");
        foreach (var kvp in workflow.Variables)
        {
            var value = kvp.Value?.ToString() ?? "";
            sb.AppendLine($"${EscapeVariableName(kvp.Key)} = {FormatPsValue(value)}");
        }
        sb.AppendLine();
    }

    private void EmitActions(StringBuilder sb, IList<IAction> actions, int indent)
    {
        foreach (var action in actions)
        {
            EmitAction(sb, action, indent);
        }
    }

    private void EmitAction(StringBuilder sb, IAction action, int indent)
    {
        var pad = new string(' ', indent * 4);

        switch (action)
        {
            case MouseClickAction mca:
                EmitMouseClick(sb, mca, pad);
                break;

            case KeyPressAction kpa:
                EmitKeyPress(sb, kpa, pad);
                break;

            case DelayAction da:
                sb.AppendLine($"{pad}# {EscapeComment(da.Name)}");
                sb.AppendLine($"{pad}Start-Sleep -Milliseconds {da.DurationMs}");
                sb.AppendLine();
                break;

            case SetVariableAction sva:
                sb.AppendLine($"{pad}# {EscapeComment(sva.Name)}");
                if (sva.EvaluateAsExpression)
                {
                    sb.AppendLine($"{pad}${EscapeVariableName(sva.VariableName)} = ({EscapePsString(sva.Value)})");
                }
                else
                {
                    sb.AppendLine($"{pad}${EscapeVariableName(sva.VariableName)} = {FormatPsValue(sva.Value)}");
                }
                sb.AppendLine();
                break;

            case RepeatAction ra:
                EmitRepeat(sb, ra, indent, pad);
                break;

            case WhileAction wa:
                EmitWhile(sb, wa, indent, pad);
                break;

            case ConditionAction ca:
                EmitCondition(sb, ca, indent, pad);
                break;

            case BreakAction:
                sb.AppendLine($"{pad}break");
                break;

            case ContinueAction:
                sb.AppendLine($"{pad}continue");
                break;

            case SequenceAction sa:
                EmitActions(sb, sa.Actions, indent);
                break;

            default:
                sb.AppendLine($"{pad}# Unsupported action: {EscapeComment(action.Name)} ({action.GetType().Name})");
                break;
        }
    }

    private void EmitMouseClick(StringBuilder sb, MouseClickAction mca, string pad)
    {
        sb.AppendLine($"{pad}# {EscapeComment(mca.Name)}");
        sb.AppendLine($"{pad}[NativeInput]::SetCursorPos({mca.X}, {mca.Y})");

        var (downFlag, upFlag) = mca.Button switch
        {
            MouseButton.Left => ("MOUSEEVENTF_LEFTDOWN", "MOUSEEVENTF_LEFTUP"),
            MouseButton.Right => ("MOUSEEVENTF_RIGHTDOWN", "MOUSEEVENTF_RIGHTUP"),
            MouseButton.Middle => ("MOUSEEVENTF_MIDDLEDOWN", "MOUSEEVENTF_MIDDLEUP"),
            _ => ("MOUSEEVENTF_LEFTDOWN", "MOUSEEVENTF_LEFTUP")
        };

        for (int i = 0; i < mca.ClickCount; i++)
        {
            sb.AppendLine($"{pad}[NativeInput]::mouse_event([NativeInput]::{downFlag}, 0, 0, 0, [IntPtr]::Zero)");
            sb.AppendLine($"{pad}[NativeInput]::mouse_event([NativeInput]::{upFlag}, 0, 0, 0, [IntPtr]::Zero)");
        }
        sb.AppendLine();
    }

    private void EmitKeyPress(StringBuilder sb, KeyPressAction kpa, string pad)
    {
        sb.AppendLine($"{pad}# {EscapeComment(kpa.Name)}");

        // Press modifiers down
        if (kpa.Ctrl) sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x11, 0, 0, [IntPtr]::Zero)  # Ctrl down");
        if (kpa.Alt) sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x12, 0, 0, [IntPtr]::Zero)  # Alt down");
        if (kpa.Shift) sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x10, 0, 0, [IntPtr]::Zero)  # Shift down");

        // Press and release the key
        sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x{kpa.VirtualKeyCode:X2}, 0, 0, [IntPtr]::Zero)");
        sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x{kpa.VirtualKeyCode:X2}, 0, [NativeInput]::KEYEVENTF_KEYUP, [IntPtr]::Zero)");

        // Release modifiers
        if (kpa.Shift) sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x10, 0, [NativeInput]::KEYEVENTF_KEYUP, [IntPtr]::Zero)  # Shift up");
        if (kpa.Alt) sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x12, 0, [NativeInput]::KEYEVENTF_KEYUP, [IntPtr]::Zero)  # Alt up");
        if (kpa.Ctrl) sb.AppendLine($"{pad}[NativeInput]::keybd_event(0x11, 0, [NativeInput]::KEYEVENTF_KEYUP, [IntPtr]::Zero)  # Ctrl up");
        sb.AppendLine();
    }

    private void EmitRepeat(StringBuilder sb, RepeatAction ra, int indent, string pad)
    {
        sb.AppendLine($"{pad}# {EscapeComment(ra.Name)}");
        sb.AppendLine($"{pad}for (${EscapeVariableName(ra.CounterVariable)} = 0; ${EscapeVariableName(ra.CounterVariable)} -lt {ra.RepeatCount}; ${EscapeVariableName(ra.CounterVariable)}++) {{");
        EmitActions(sb, ra.Actions, indent + 1);
        sb.AppendLine($"{pad}}}");
        sb.AppendLine();
    }

    private void EmitWhile(StringBuilder sb, WhileAction wa, int indent, string pad)
    {
        sb.AppendLine($"{pad}# {EscapeComment(wa.Name)}");
        var condStr = FormatCondition(wa.Condition);
        sb.AppendLine($"{pad}$__whileGuard = 0");
        sb.AppendLine($"{pad}while ({condStr}) {{");
        var innerPad = new string(' ', (indent + 1) * 4);
        sb.AppendLine($"{innerPad}$__whileGuard++");
        sb.AppendLine($"{innerPad}if ($__whileGuard -gt {wa.MaxIterations}) {{ throw \"While loop exceeded maximum iterations ({wa.MaxIterations})\" }}");
        EmitActions(sb, wa.Actions, indent + 1);
        sb.AppendLine($"{pad}}}");
        sb.AppendLine();
    }

    private void EmitCondition(StringBuilder sb, ConditionAction ca, int indent, string pad)
    {
        sb.AppendLine($"{pad}# {EscapeComment(ca.Name)}");
        var condStr = FormatCondition(ca.Condition);
        sb.AppendLine($"{pad}if ({condStr}) {{");
        EmitActions(sb, ca.ThenActions, indent + 1);

        if (ca.ElseActions.Count > 0)
        {
            sb.AppendLine($"{pad}}} else {{");
            EmitActions(sb, ca.ElseActions, indent + 1);
        }
        sb.AppendLine($"{pad}}}");
        sb.AppendLine();
    }

    private string FormatCondition(Condition condition)
    {
        var left = FormatOperand(condition.LeftOperand);
        var right = FormatOperand(condition.RightOperand);
        var op = MapOperator(condition.Operator);

        var result = $"{left} {op} {right}";

        if (condition.LogicalOp != null && condition.NextCondition != null)
        {
            var logicalOp = condition.LogicalOp == LogicalOperator.And ? "-and" : "-or";
            result += $" {logicalOp} {FormatCondition(condition.NextCondition)}";
        }

        return result;
    }

    private string FormatOperand(string operand)
    {
        if (string.IsNullOrEmpty(operand)) return "\"\"";

        // Variable reference: {varName} → $varName
        if (operand.StartsWith("{") && operand.EndsWith("}"))
        {
            var varName = operand[1..^1];
            return $"${EscapeVariableName(varName)}";
        }

        // Numeric
        if (double.TryParse(operand, out _))
        {
            return operand;
        }

        // String
        return FormatPsValue(operand);
    }

    private static string MapOperator(ComparisonOperator op) => op switch
    {
        ComparisonOperator.Equals => "-eq",
        ComparisonOperator.NotEquals => "-ne",
        ComparisonOperator.LessThan => "-lt",
        ComparisonOperator.GreaterThan => "-gt",
        ComparisonOperator.LessOrEqual => "-le",
        ComparisonOperator.GreaterOrEqual => "-ge",
        ComparisonOperator.Contains => "-like",
        ComparisonOperator.StartsWith => "-like",
        ComparisonOperator.EndsWith => "-like",
        _ => "-eq"
    };

    private static string FormatPsValue(string value)
    {
        // Escape single quotes by doubling them
        return $"'{value.Replace("'", "''")}'";
    }

    private static string EscapeVariableName(string name)
    {
        // Remove characters invalid in PS variable names
        return name.Replace(" ", "_").Replace("-", "_");
    }

    private static string EscapeComment(string text)
    {
        return text.Replace("\n", " ").Replace("\r", "");
    }

    private static string EscapePsString(string text)
    {
        return text.Replace("\"", "`\"");
    }
}
