using UniversalTasker.Core.Actions;

namespace UniversalTasker.Tests.Actions;

public class RepeatActionTests
{
    [Fact]
    public async Task ExecuteAsync_RepeatsCorrectNumberOfTimes()
    {
        int count = 0;
        var repeat = new RepeatAction { RepeatCount = 5 };
        repeat.Actions.Add(new CallbackAction(() => count++));

        var context = new ExecutionContext();
        await repeat.ExecuteAsync(context);

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task ExecuteAsync_SetsCounterVariable()
    {
        var indices = new List<object?>();
        var repeat = new RepeatAction { RepeatCount = 3, CounterVariable = "idx" };
        repeat.Actions.Add(new CallbackAction(ctx => indices.Add(ctx.Variables["idx"])));

        var context = new ExecutionContext();
        await repeat.ExecuteAsync(context);

        Assert.Equal(new object[] { 0, 1, 2 }, indices);
    }

    [Fact]
    public async Task ExecuteAsync_BreakAction_StopsLoop()
    {
        int count = 0;
        var repeat = new RepeatAction { RepeatCount = 100 };
        repeat.Actions.Add(new CallbackAction(ctx =>
        {
            count++;
            if (count >= 3) throw new LoopBreakException();
        }));

        var context = new ExecutionContext();
        await repeat.ExecuteAsync(context);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ExecuteAsync_ContinueAction_SkipsRemainder()
    {
        var values = new List<int>();
        var repeat = new RepeatAction { RepeatCount = 5, CounterVariable = "i" };
        repeat.Actions.Add(new CallbackAction(ctx =>
        {
            var i = (int)ctx.Variables["i"]!;
            if (i % 2 == 0) throw new LoopContinueException();
        }));
        repeat.Actions.Add(new CallbackAction(ctx =>
        {
            var i = (int)ctx.Variables["i"]!;
            values.Add(i);
        }));

        var context = new ExecutionContext();
        await repeat.ExecuteAsync(context);

        Assert.Equal([1, 3], values);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroRepeatCount_DoesNothing()
    {
        int count = 0;
        var repeat = new RepeatAction { RepeatCount = 0 };
        repeat.Actions.Add(new CallbackAction(() => count++));

        var context = new ExecutionContext();
        await repeat.ExecuteAsync(context);

        Assert.Equal(0, count);
    }
}

file class CallbackAction : ActionBase
{
    private readonly Action<ExecutionContext>? _ctxCallback;
    private readonly Action? _callback;
    public override string Category => "Test";

    public CallbackAction(Action callback) { _callback = callback; Name = "Callback"; }
    public CallbackAction(Action<ExecutionContext> callback) { _ctxCallback = callback; Name = "Callback"; }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);
        _callback?.Invoke();
        _ctxCallback?.Invoke(context);
        return Task.CompletedTask;
    }
}
