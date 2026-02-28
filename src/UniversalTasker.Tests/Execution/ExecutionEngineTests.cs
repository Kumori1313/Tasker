using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Execution;

namespace UniversalTasker.Tests.Execution;

public class ExecutionEngineTests
{
    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var engine = new ExecutionEngine();
        var actions = new List<IAction> { new DelayAction { DurationMs = 10 } };

        await engine.StartAsync(actions);

        Assert.Equal(ExecutionState.Completed, engine.State);
    }

    [Fact]
    public async Task StartAsync_EmptyList_Completes()
    {
        var engine = new ExecutionEngine();

        await engine.StartAsync(Array.Empty<IAction>());

        Assert.Equal(ExecutionState.Completed, engine.State);
    }

    [Fact]
    public async Task StartAsync_FiresExecutionStarted()
    {
        var engine = new ExecutionEngine();
        bool started = false;
        engine.ExecutionStarted += (_, _) => started = true;

        await engine.StartAsync(new[] { new DelayAction { DurationMs = 10 } });

        Assert.True(started);
    }

    [Fact]
    public async Task StartAsync_FiresExecutionCompleted()
    {
        var engine = new ExecutionEngine();
        ExecutionCompletedEventArgs? completed = null;
        engine.ExecutionCompleted += (_, e) => completed = e;

        await engine.StartAsync(new[] { new DelayAction { DurationMs = 10 } });

        Assert.NotNull(completed);
        Assert.Equal(ExecutionState.Completed, completed!.FinalState);
        Assert.Null(completed.Exception);
    }

    [Fact]
    public async Task StartAsync_FiresActionExecuting_ForEachAction()
    {
        var engine = new ExecutionEngine();
        var indices = new List<int>();
        engine.ActionExecuting += (_, e) => indices.Add(e.Index);

        var actions = new List<IAction>
        {
            new DelayAction { DurationMs = 10 },
            new DelayAction { DurationMs = 10 },
            new DelayAction { DurationMs = 10 }
        };

        await engine.StartAsync(actions);

        Assert.Equal([0, 1, 2], indices);
    }

    [Fact]
    public async Task Stop_CancelsExecution()
    {
        var engine = new ExecutionEngine();
        var actions = new List<IAction> { new DelayAction { DurationMs = 10000 } };

        var task = engine.StartAsync(actions);

        // Give it a moment to start
        await Task.Delay(50);
        engine.Stop();

        await task;

        Assert.Equal(ExecutionState.Cancelled, engine.State);
    }

    [Fact]
    public async Task Pause_Resume_ContinuesExecution()
    {
        var engine = new ExecutionEngine();
        var order = new List<string>();

        var actions = new List<IAction>
        {
            new CallbackAction(() => order.Add("first")),
            new DelayAction { DurationMs = 100 },
            new CallbackAction(() => order.Add("second"))
        };

        var task = engine.StartAsync(actions);

        // Give first action time to execute
        await Task.Delay(30);
        engine.Pause();
        Assert.Equal(ExecutionState.Paused, engine.State);

        await Task.Delay(50);
        engine.Resume();

        await task;

        Assert.Equal(ExecutionState.Completed, engine.State);
        Assert.Equal(["first", "second"], order);
    }

    [Fact]
    public async Task StartAsync_ActionThrows_StateIsFailed()
    {
        var engine = new ExecutionEngine();
        var actions = new List<IAction> { new ThrowingAction() };

        await engine.StartAsync(actions);

        Assert.Equal(ExecutionState.Failed, engine.State);
    }

    [Fact]
    public async Task StartAsync_ActionThrows_CompletedEventHasException()
    {
        var engine = new ExecutionEngine();
        ExecutionCompletedEventArgs? completed = null;
        engine.ExecutionCompleted += (_, e) => completed = e;

        await engine.StartAsync(new IAction[] { new ThrowingAction() });

        Assert.NotNull(completed);
        Assert.Equal(ExecutionState.Failed, completed!.FinalState);
        Assert.NotNull(completed.Exception);
    }

    [Fact]
    public void InitialState_IsIdle()
    {
        var engine = new ExecutionEngine();
        Assert.Equal(ExecutionState.Idle, engine.State);
    }

    [Fact]
    public async Task StartAsync_WhileRunning_Throws()
    {
        var engine = new ExecutionEngine();
        var actions = new List<IAction> { new DelayAction { DurationMs = 5000 } };

        var task = engine.StartAsync(actions);
        await Task.Delay(50);

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.StartAsync(actions));

        engine.Stop();
        await task;
    }
}

file class CallbackAction : ActionBase
{
    private readonly Action _callback;
    public override string Category => "Test";

    public CallbackAction(Action callback) { _callback = callback; Name = "Callback"; }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);
        _callback();
        return Task.CompletedTask;
    }
}

file class ThrowingAction : ActionBase
{
    public override string Category => "Test";
    public ThrowingAction() { Name = "Thrower"; }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        throw new InvalidOperationException("Test error");
    }
}
