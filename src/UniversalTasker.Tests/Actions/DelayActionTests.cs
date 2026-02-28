using UniversalTasker.Core.Actions;

namespace UniversalTasker.Tests.Actions;

public class DelayActionTests
{
    [Fact]
    public async Task ExecuteAsync_WaitsForDuration()
    {
        var action = new DelayAction { DurationMs = 50 };
        var context = new ExecutionContext();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await action.ExecuteAsync(context);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 40, $"Expected >= 40ms, got {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCanellationToken()
    {
        var action = new DelayAction { DurationMs = 5000 };
        using var cts = new CancellationTokenSource(50);
        var context = new ExecutionContext(cts.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => action.ExecuteAsync(context));
    }

    [Fact]
    public void DefaultDuration_Is1000()
    {
        var action = new DelayAction();
        Assert.Equal(1000, action.DurationMs);
    }

    [Fact]
    public void Category_IsFlow()
    {
        var action = new DelayAction();
        Assert.Equal("Flow", action.Category);
    }
}
