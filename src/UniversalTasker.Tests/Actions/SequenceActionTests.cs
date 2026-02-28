using UniversalTasker.Core.Actions;

namespace UniversalTasker.Tests.Actions;

public class SequenceActionTests
{
    [Fact]
    public async Task ExecuteAsync_RunsAllActionsInOrder()
    {
        var order = new List<int>();

        var sequence = new SequenceAction();
        sequence.Actions.Add(new TrackingAction(() => order.Add(1)));
        sequence.Actions.Add(new TrackingAction(() => order.Add(2)));
        sequence.Actions.Add(new TrackingAction(() => order.Add(3)));

        var context = new ExecutionContext();
        await sequence.ExecuteAsync(context);

        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySequence_Completes()
    {
        var sequence = new SequenceAction();
        var context = new ExecutionContext();

        await sequence.ExecuteAsync(context);
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesExceptions()
    {
        var sequence = new SequenceAction();
        sequence.Actions.Add(new ThrowingAction());

        var context = new ExecutionContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sequence.ExecuteAsync(context));
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCanellationToken()
    {
        using var cts = new CancellationTokenSource();
        var context = new ExecutionContext(cts.Token);

        var sequence = new SequenceAction();
        sequence.Actions.Add(new TrackingAction(() => cts.Cancel()));
        sequence.Actions.Add(new DelayAction { DurationMs = 5000 });

        await Assert.ThrowsAsync<OperationCanceledException>(() => sequence.ExecuteAsync(context));
    }
}

/// <summary>Helper action that runs a callback.</summary>
file class TrackingAction : ActionBase
{
    private readonly Action _callback;
    public override string Category => "Test";

    public TrackingAction(Action callback) { _callback = callback; Name = "Tracking"; }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);
        _callback();
        return Task.CompletedTask;
    }
}

/// <summary>Helper action that always throws.</summary>
file class ThrowingAction : ActionBase
{
    public override string Category => "Test";
    public ThrowingAction() { Name = "Thrower"; }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        throw new InvalidOperationException("Test exception");
    }
}
