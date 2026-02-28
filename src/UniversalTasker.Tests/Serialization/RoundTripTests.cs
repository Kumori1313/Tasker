using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;
using UniversalTasker.Core.Triggers;
using UniversalTasker.Core.Workflows;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public class RoundTripTests
{
    private readonly WorkflowSerializer _serializer = new();

    /// <summary>
    /// Serializes a workflow, deserializes it, serializes again, and verifies
    /// both JSON outputs are identical — proving no data loss through the cycle.
    /// </summary>
    private void AssertRoundTripStable(Workflow workflow)
    {
        var json1 = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json1);
        var json2 = _serializer.Serialize(deserialized);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void RoundTrip_EmptyWorkflow_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-empty",
            Name = "Empty Workflow",
            Description = "No actions or triggers",
            Version = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_AllSettings_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-settings",
            Name = "Settings Test",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Settings = new WorkflowSettings
            {
                StopOnError = false,
                MaxExecutionTimeSeconds = 300,
                LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                EnableTriggersOnStart = false,
                AllowConcurrentExecution = true
            }
        };

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_Variables_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-vars",
            Name = "Variables Test",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        workflow.Variables["counter"] = 42;
        workflow.Variables["name"] = "test-value";
        workflow.Variables["flag"] = true;
        workflow.Variables["rate"] = 3.14;

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_AllActionTypes_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-actions",
            Name = "All Actions",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Delay
        workflow.Actions.Add(new DelayAction { Name = "Wait", DurationMs = 500 });

        // MouseClick
        workflow.Actions.Add(new MouseClickAction
        {
            Name = "Click",
            X = 100, Y = 200,
            Button = Core.Input.MouseButton.Right,
            ClickCount = 2
        });

        // KeyPress
        workflow.Actions.Add(new KeyPressAction
        {
            Name = "Press",
            VirtualKeyCode = 0x41,
            Ctrl = true,
            Alt = false,
            Shift = true
        });

        // SetVariable
        workflow.Actions.Add(new SetVariableAction
        {
            Name = "Set Var",
            VariableName = "x",
            Value = "hello",
            EvaluateAsExpression = false
        });

        // Break
        workflow.Actions.Add(new BreakAction { Name = "Exit Loop" });

        // Continue
        workflow.Actions.Add(new ContinueAction { Name = "Next Iteration" });

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_NestedContainers_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-nested",
            Name = "Nested Containers",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Repeat with children
        var repeat = new RepeatAction
        {
            Name = "Outer Repeat",
            RepeatCount = 5,
            CounterVariable = "i"
        };

        // Nested while inside repeat
        var whileLoop = new WhileAction
        {
            Name = "Inner While",
            MaxIterations = 100,
            Condition = new Condition
            {
                LeftOperand = "{x}",
                Operator = ComparisonOperator.LessThan,
                RightOperand = "10"
            }
        };
        whileLoop.Actions.Add(new DelayAction { Name = "Inner Delay", DurationMs = 50 });
        whileLoop.Actions.Add(new SetVariableAction
        {
            Name = "Increment",
            VariableName = "x",
            Value = "{x} + 1",
            EvaluateAsExpression = true
        });

        repeat.Actions.Add(whileLoop);
        repeat.Actions.Add(new DelayAction { Name = "Between", DurationMs = 100 });

        workflow.Actions.Add(repeat);

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_ConditionWithBranches_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-condition",
            Name = "Condition Test",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var condition = new ConditionAction
        {
            Name = "Check Value",
            Condition = new Condition
            {
                LeftOperand = "{count}",
                Operator = ComparisonOperator.GreaterOrEqual,
                RightOperand = "5"
            }
        };
        condition.ThenActions.Add(new DelayAction { Name = "Then Delay", DurationMs = 100 });
        condition.ThenActions.Add(new BreakAction { Name = "Then Break" });
        condition.ElseActions.Add(new DelayAction { Name = "Else Delay", DurationMs = 200 });
        condition.ElseActions.Add(new ContinueAction { Name = "Else Continue" });

        workflow.Actions.Add(condition);

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_AllTriggerTypes_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-triggers",
            Name = "All Triggers",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        workflow.Triggers.Add(new TimerTrigger
        {
            Name = "Timer",
            Interval = TimeSpan.FromSeconds(30),
            FireImmediately = true,
            IsEnabled = true
        });

        workflow.Triggers.Add(new FileSystemTrigger
        {
            Name = "File Watcher",
            Path = @"C:\Temp",
            Filter = "*.log",
            IncludeSubdirectories = true,
            WatchCreated = true,
            WatchChanged = false,
            WatchDeleted = true,
            WatchRenamed = false,
            IsEnabled = true
        });

        workflow.Triggers.Add(new HotkeyTrigger
        {
            Name = "Ctrl+Shift+F5",
            VirtualKeyCode = 0x74,
            Ctrl = true,
            Alt = false,
            Shift = true,
            IsEnabled = false
        });

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_TriggerWithConfiguration_IsStable()
    {
        var workflow = new Workflow
        {
            Id = "rt-trigger-config",
            Name = "Trigger Config",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var timer = new TimerTrigger
        {
            Name = "Configured Timer",
            Interval = TimeSpan.FromMinutes(5)
        };
        timer.Configuration.DebounceTime = TimeSpan.FromSeconds(2);
        timer.Configuration.MaxFireCount = 50;
        timer.Configuration.EnabledOnStart = false;

        workflow.Triggers.Add(timer);

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public void RoundTrip_CompleteWorkflow_IsStable()
    {
        // Build a realistic workflow with everything
        var workflow = new Workflow
        {
            Id = "rt-complete",
            Name = "Full Automation",
            Description = "A comprehensive test workflow",
            Version = 3,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Settings = new WorkflowSettings
            {
                StopOnError = false,
                MaxExecutionTimeSeconds = 60,
                LogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
                AllowConcurrentExecution = true,
                EnableTriggersOnStart = false
            }
        };

        workflow.Variables["startX"] = 100;
        workflow.Variables["startY"] = 200;
        workflow.Variables["label"] = "automation";

        workflow.Triggers.Add(new TimerTrigger
        {
            Name = "Periodic",
            Interval = TimeSpan.FromSeconds(10),
            FireImmediately = false
        });

        workflow.Triggers.Add(new HotkeyTrigger
        {
            Name = "Manual Start",
            VirtualKeyCode = 0x75,
            Ctrl = true
        });

        // Actions: set variable, repeat with condition inside
        workflow.Actions.Add(new SetVariableAction
        {
            Name = "Init Counter",
            VariableName = "i",
            Value = "0"
        });

        var repeat = new RepeatAction
        {
            Name = "Main Loop",
            RepeatCount = 10,
            CounterVariable = "i"
        };

        var condition = new ConditionAction
        {
            Name = "Check i",
            Condition = new Condition
            {
                LeftOperand = "{i}",
                Operator = ComparisonOperator.Equals,
                RightOperand = "5"
            }
        };
        condition.ThenActions.Add(new BreakAction { Name = "Stop at 5" });
        condition.ElseActions.Add(new MouseClickAction
        {
            Name = "Click at position",
            X = 500, Y = 300,
            Button = Core.Input.MouseButton.Left,
            ClickCount = 1
        });

        repeat.Actions.Add(condition);
        repeat.Actions.Add(new DelayAction { Name = "Loop Delay", DurationMs = 250 });

        workflow.Actions.Add(repeat);

        AssertRoundTripStable(workflow);
    }

    [Fact]
    public async Task RoundTrip_SaveLoadSave_FileContentsMatch()
    {
        var workflow = new Workflow
        {
            Id = "rt-file",
            Name = "File Round Trip",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        workflow.Actions.Add(new DelayAction { Name = "Wait", DurationMs = 1000 });
        workflow.Triggers.Add(new TimerTrigger { Name = "Timer", Interval = TimeSpan.FromSeconds(5) });
        workflow.Variables["key"] = "value";

        var tempPath = Path.Combine(Path.GetTempPath(), $"tasker_rt_test_{Guid.NewGuid()}.json");
        try
        {
            // Save → Load → Save
            await _serializer.SaveAsync(workflow, tempPath);
            var content1 = await File.ReadAllTextAsync(tempPath);

            var loaded = await _serializer.LoadAsync(tempPath);
            await _serializer.SaveAsync(loaded, tempPath);
            var content2 = await File.ReadAllTextAsync(tempPath);

            Assert.Equal(content1, content2);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
