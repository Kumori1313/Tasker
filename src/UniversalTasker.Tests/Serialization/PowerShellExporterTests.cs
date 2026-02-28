using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;
using UniversalTasker.Core.Input;
using UniversalTasker.Core.Workflows;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public class PowerShellExporterTests
{
    private readonly PowerShellExporter _exporter = new();

    [Fact]
    public void Export_EmptyWorkflow_ProducesValidHeader()
    {
        var workflow = new Workflow { Name = "Empty" };

        var result = _exporter.Export(workflow);

        Assert.Contains("# PowerShell automation script", result);
        Assert.Contains("# Generated from workflow: Empty", result);
        Assert.Contains("Add-Type", result);
        Assert.Contains("NativeInput", result);
    }

    [Fact]
    public void Export_SingleDelay_ContainsStartSleep()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new DelayAction { DurationMs = 1000 });

        var result = _exporter.Export(workflow);

        Assert.Contains("Start-Sleep -Milliseconds 1000", result);
    }

    [Fact]
    public void Export_MouseClick_ContainsSetCursorPosAndMouseEvent()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new MouseClickAction { X = 100, Y = 200, Button = MouseButton.Left, ClickCount = 1 });

        var result = _exporter.Export(workflow);

        Assert.Contains("[NativeInput]::SetCursorPos(100, 200)", result);
        Assert.Contains("MOUSEEVENTF_LEFTDOWN", result);
        Assert.Contains("MOUSEEVENTF_LEFTUP", result);
    }

    [Fact]
    public void Export_RightMouseClick_UsesRightFlags()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new MouseClickAction { X = 50, Y = 50, Button = MouseButton.Right, ClickCount = 1 });

        var result = _exporter.Export(workflow);

        Assert.Contains("MOUSEEVENTF_RIGHTDOWN", result);
        Assert.Contains("MOUSEEVENTF_RIGHTUP", result);
    }

    [Fact]
    public void Export_KeyPress_ContainsKeybdEvent()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new KeyPressAction { VirtualKeyCode = 0x41 }); // 'A'

        var result = _exporter.Export(workflow);

        Assert.Contains("keybd_event(0x41", result);
        Assert.Contains("KEYEVENTF_KEYUP", result);
    }

    [Fact]
    public void Export_KeyPressWithModifiers_EmitsModifierKeysDownAndUp()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new KeyPressAction { VirtualKeyCode = 0x41, Ctrl = true, Shift = true });

        var result = _exporter.Export(workflow);

        Assert.Contains("0x11", result); // Ctrl
        Assert.Contains("0x10", result); // Shift
        Assert.Contains("Ctrl down", result);
        Assert.Contains("Shift down", result);
        Assert.Contains("Shift up", result);
        Assert.Contains("Ctrl up", result);
    }

    [Fact]
    public void Export_RepeatAction_ContainsForLoop()
    {
        var workflow = new Workflow { Name = "Test" };
        var repeat = new RepeatAction { RepeatCount = 3, CounterVariable = "i" };
        repeat.Actions.Add(new DelayAction { DurationMs = 500 });
        workflow.Actions.Add(repeat);

        var result = _exporter.Export(workflow);

        Assert.Contains("for ($i = 0; $i -lt 3; $i++)", result);
        Assert.Contains("Start-Sleep -Milliseconds 500", result);
    }

    [Fact]
    public void Export_WhileAction_ContainsWhileLoop()
    {
        var workflow = new Workflow { Name = "Test" };
        var whileAction = new WhileAction
        {
            Condition = new Condition
            {
                LeftOperand = "{counter}",
                Operator = ComparisonOperator.LessThan,
                RightOperand = "10"
            },
            MaxIterations = 100
        };
        whileAction.Actions.Add(new DelayAction { DurationMs = 100 });
        workflow.Actions.Add(whileAction);

        var result = _exporter.Export(workflow);

        Assert.Contains("while ($counter -lt 10)", result);
        Assert.Contains("$__whileGuard", result);
        Assert.Contains("100", result);
    }

    [Fact]
    public void Export_ConditionAction_ContainsIfElse()
    {
        var workflow = new Workflow { Name = "Test" };
        var condition = new ConditionAction
        {
            Condition = new Condition
            {
                LeftOperand = "{x}",
                Operator = ComparisonOperator.Equals,
                RightOperand = "5"
            }
        };
        condition.ThenActions.Add(new DelayAction { Name = "Then Delay", DurationMs = 100 });
        condition.ElseActions.Add(new DelayAction { Name = "Else Delay", DurationMs = 200 });
        workflow.Actions.Add(condition);

        var result = _exporter.Export(workflow);

        Assert.Contains("if ($x -eq 5)", result);
        Assert.Contains("} else {", result);
        Assert.Contains("Start-Sleep -Milliseconds 100", result);
        Assert.Contains("Start-Sleep -Milliseconds 200", result);
    }

    [Fact]
    public void Export_ConditionWithoutElse_NoElseBlock()
    {
        var workflow = new Workflow { Name = "Test" };
        var condition = new ConditionAction
        {
            Condition = new Condition
            {
                LeftOperand = "{x}",
                Operator = ComparisonOperator.GreaterThan,
                RightOperand = "0"
            }
        };
        condition.ThenActions.Add(new DelayAction { DurationMs = 100 });
        workflow.Actions.Add(condition);

        var result = _exporter.Export(workflow);

        Assert.Contains("if ($x -gt 0)", result);
        Assert.DoesNotContain("} else {", result);
    }

    [Fact]
    public void Export_Variables_EmitsVariableAssignments()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Variables["userName"] = "John";
        workflow.Variables["count"] = 5;

        var result = _exporter.Export(workflow);

        Assert.Contains("$userName = 'John'", result);
        Assert.Contains("$count = '5'", result);
    }

    [Fact]
    public void Export_SetVariable_EmitsAssignment()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new SetVariableAction
        {
            VariableName = "result",
            Value = "hello",
            EvaluateAsExpression = false
        });

        var result = _exporter.Export(workflow);

        Assert.Contains("$result = 'hello'", result);
    }

    [Fact]
    public void Export_BreakAction_EmitsBreak()
    {
        var workflow = new Workflow { Name = "Test" };
        var repeat = new RepeatAction { RepeatCount = 10 };
        repeat.Actions.Add(new BreakAction());
        workflow.Actions.Add(repeat);

        var result = _exporter.Export(workflow);

        Assert.Contains("break", result);
    }

    [Fact]
    public void Export_ContinueAction_EmitsContinue()
    {
        var workflow = new Workflow { Name = "Test" };
        var repeat = new RepeatAction { RepeatCount = 10 };
        repeat.Actions.Add(new ContinueAction());
        workflow.Actions.Add(repeat);

        var result = _exporter.Export(workflow);

        Assert.Contains("continue", result);
    }

    [Fact]
    public void Export_DoubleClick_EmitsTwoClickPairs()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new MouseClickAction { X = 10, Y = 20, Button = MouseButton.Left, ClickCount = 2 });

        var result = _exporter.Export(workflow);

        // Count occurrences of mouse_event calls with LEFTDOWN — should be 2 for double-click
        // The header also defines the constant, so count only the invocation lines
        var lines = result.Split('\n').Where(l => l.Contains("::mouse_event") && l.Contains("LEFTDOWN")).ToArray();
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public void Export_AllOperators_MappedCorrectly()
    {
        var operators = new (ComparisonOperator Op, string Expected)[]
        {
            (ComparisonOperator.Equals, "-eq"),
            (ComparisonOperator.NotEquals, "-ne"),
            (ComparisonOperator.LessThan, "-lt"),
            (ComparisonOperator.GreaterThan, "-gt"),
            (ComparisonOperator.LessOrEqual, "-le"),
            (ComparisonOperator.GreaterOrEqual, "-ge"),
        };

        foreach (var (op, expected) in operators)
        {
            var workflow = new Workflow { Name = "Test" };
            var condition = new ConditionAction
            {
                Condition = new Condition
                {
                    LeftOperand = "1",
                    Operator = op,
                    RightOperand = "2"
                }
            };
            condition.ThenActions.Add(new DelayAction { DurationMs = 1 });
            workflow.Actions.Add(condition);

            var result = _exporter.Export(workflow);
            Assert.Contains(expected, result);
        }
    }

    [Fact]
    public void Export_SequenceAction_FlattensChildren()
    {
        var workflow = new Workflow { Name = "Test" };
        var seq = new SequenceAction();
        seq.Actions.Add(new DelayAction { DurationMs = 100 });
        seq.Actions.Add(new DelayAction { DurationMs = 200 });
        workflow.Actions.Add(seq);

        var result = _exporter.Export(workflow);

        Assert.Contains("Start-Sleep -Milliseconds 100", result);
        Assert.Contains("Start-Sleep -Milliseconds 200", result);
    }
}
