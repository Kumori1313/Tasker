using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;
using UniversalTasker.Core.Triggers;
using UniversalTasker.Core.Workflows;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public class WorkflowSerializerTests
{
    private readonly WorkflowSerializer _serializer = new();

    [Fact]
    public void Serialize_Deserialize_RoundTrip_PreservesWorkflowProperties()
    {
        var workflow = new Workflow
        {
            Id = "test-001",
            Name = "Test Workflow",
            Description = "A test workflow",
            Version = 2,
            Settings = new WorkflowSettings
            {
                StopOnError = false,
                MaxExecutionTimeSeconds = 120,
                LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
                AllowConcurrentExecution = true
            }
        };

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        Assert.Equal("test-001", deserialized.Id);
        Assert.Equal("Test Workflow", deserialized.Name);
        Assert.Equal("A test workflow", deserialized.Description);
        Assert.Equal(2, deserialized.Version);
        Assert.False(deserialized.Settings.StopOnError);
        Assert.Equal(120, deserialized.Settings.MaxExecutionTimeSeconds);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, deserialized.Settings.LogLevel);
        Assert.True(deserialized.Settings.AllowConcurrentExecution);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_PreservesVariables()
    {
        var workflow = new Workflow();
        workflow.Variables["counter"] = 0;
        workflow.Variables["name"] = "test";
        workflow.Variables["enabled"] = true;

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        Assert.Equal(0, deserialized.Variables["counter"]);
        Assert.Equal("test", deserialized.Variables["name"]);
        Assert.Equal(true, deserialized.Variables["enabled"]);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_DelayAction()
    {
        var workflow = new Workflow();
        workflow.Actions.Add(new DelayAction { Name = "Wait 500ms", DurationMs = 500 });

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        Assert.Single(deserialized.Actions);
        var delay = Assert.IsType<DelayAction>(deserialized.Actions[0]);
        Assert.Equal("Wait 500ms", delay.Name);
        Assert.Equal(500, delay.DurationMs);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_MouseClickAction()
    {
        var workflow = new Workflow();
        workflow.Actions.Add(new MouseClickAction
        {
            Name = "Click Button",
            X = 100, Y = 200,
            Button = Core.Input.MouseButton.Right,
            ClickCount = 2
        });

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        var action = Assert.IsType<MouseClickAction>(deserialized.Actions[0]);
        Assert.Equal(100, action.X);
        Assert.Equal(200, action.Y);
        Assert.Equal(Core.Input.MouseButton.Right, action.Button);
        Assert.Equal(2, action.ClickCount);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_RepeatAction()
    {
        var repeat = new RepeatAction
        {
            Name = "Loop 3 times",
            RepeatCount = 3,
            CounterVariable = "idx"
        };
        repeat.Actions.Add(new DelayAction { DurationMs = 100 });

        var workflow = new Workflow();
        workflow.Actions.Add(repeat);

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        var action = Assert.IsType<RepeatAction>(deserialized.Actions[0]);
        Assert.Equal(3, action.RepeatCount);
        Assert.Equal("idx", action.CounterVariable);
        Assert.Single(action.Actions);
        Assert.IsType<DelayAction>(action.Actions[0]);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_ConditionAction()
    {
        var condition = new ConditionAction
        {
            Name = "Check value",
            Condition = new Condition
            {
                LeftOperand = "{x}",
                Operator = ComparisonOperator.GreaterThan,
                RightOperand = "10"
            }
        };
        condition.ThenActions.Add(new DelayAction { DurationMs = 100 });
        condition.ElseActions.Add(new DelayAction { DurationMs = 200 });

        var workflow = new Workflow();
        workflow.Actions.Add(condition);

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        var action = Assert.IsType<ConditionAction>(deserialized.Actions[0]);
        Assert.Equal("{x}", action.Condition.LeftOperand);
        Assert.Equal(ComparisonOperator.GreaterThan, action.Condition.Operator);
        Assert.Equal("10", action.Condition.RightOperand);
        Assert.Single(action.ThenActions);
        Assert.Single(action.ElseActions);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_TimerTrigger()
    {
        var workflow = new Workflow();
        workflow.Triggers.Add(new TimerTrigger
        {
            Name = "Every 5 seconds",
            Interval = TimeSpan.FromSeconds(5),
            FireImmediately = true
        });

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        Assert.Single(deserialized.Triggers);
        var trigger = Assert.IsType<TimerTrigger>(deserialized.Triggers[0]);
        Assert.Equal("Every 5 seconds", trigger.Name);
        Assert.Equal(TimeSpan.FromSeconds(5), trigger.Interval);
        Assert.True(trigger.FireImmediately);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_FileSystemTrigger()
    {
        var workflow = new Workflow();
        workflow.Triggers.Add(new FileSystemTrigger
        {
            Name = "Watch downloads",
            Path = @"C:\Downloads",
            Filter = "*.txt",
            IncludeSubdirectories = true,
            WatchCreated = true,
            WatchChanged = false,
            WatchDeleted = false,
            WatchRenamed = false
        });

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        var trigger = Assert.IsType<FileSystemTrigger>(deserialized.Triggers[0]);
        Assert.Equal("Watch downloads", trigger.Name);
        Assert.Equal(@"C:\Downloads", trigger.Path);
        Assert.Equal("*.txt", trigger.Filter);
        Assert.True(trigger.IncludeSubdirectories);
        Assert.True(trigger.WatchCreated);
        Assert.False(trigger.WatchChanged);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_HotkeyTrigger()
    {
        var workflow = new Workflow();
        workflow.Triggers.Add(new HotkeyTrigger
        {
            Name = "Ctrl+F5",
            VirtualKeyCode = 0x74, // F5
            Ctrl = true,
            Alt = false,
            Shift = false
        });

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        var trigger = Assert.IsType<HotkeyTrigger>(deserialized.Triggers[0]);
        Assert.Equal("Ctrl+F5", trigger.Name);
        Assert.Equal(0x74, trigger.VirtualKeyCode);
        Assert.True(trigger.Ctrl);
        Assert.False(trigger.Alt);
    }

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var workflow = new Workflow { Name = "Test" };
        workflow.Actions.Add(new DelayAction { DurationMs = 100 });

        var json = _serializer.Serialize(workflow);

        // Should not throw
        System.Text.Json.JsonDocument.Parse(json);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_TriggerConfiguration()
    {
        var workflow = new Workflow();
        var trigger = new TimerTrigger
        {
            Name = "Configured Timer",
            Interval = TimeSpan.FromSeconds(1)
        };
        trigger.Configuration.DebounceTime = TimeSpan.FromMilliseconds(500);
        trigger.Configuration.MaxFireCount = 10;
        trigger.Configuration.EnabledOnStart = false;
        workflow.Triggers.Add(trigger);

        var json = _serializer.Serialize(workflow);
        var deserialized = _serializer.Deserialize(json);

        var dt = Assert.IsType<TimerTrigger>(deserialized.Triggers[0]);
        Assert.Equal(500, dt.Configuration.DebounceTime.TotalMilliseconds);
        Assert.Equal(10, dt.Configuration.MaxFireCount);
        Assert.False(dt.Configuration.EnabledOnStart);
    }
}
