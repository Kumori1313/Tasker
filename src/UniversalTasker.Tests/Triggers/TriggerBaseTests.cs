using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Tests.Triggers;

public class TriggerBaseTests
{
    [Fact]
    public void Start_SetsIsRunning()
    {
        using var trigger = new TestTrigger();

        trigger.Start();

        Assert.True(trigger.IsRunning);
    }

    [Fact]
    public void Stop_ClearsIsRunning()
    {
        using var trigger = new TestTrigger();
        trigger.Start();

        trigger.Stop();

        Assert.False(trigger.IsRunning);
    }

    [Fact]
    public void Start_WhenDisabled_DoesNotStart()
    {
        using var trigger = new TestTrigger { IsEnabled = false };

        trigger.Start();

        Assert.False(trigger.IsRunning);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_DoesNotRestartTwice()
    {
        using var trigger = new TestTrigger();
        trigger.Start();

        trigger.Start(); // should be no-op

        Assert.Equal(1, trigger.StartCount);
    }

    [Fact]
    public void RaiseFired_InvokesFiredEvent()
    {
        using var trigger = new TestTrigger();
        TriggerFiredEventArgs? received = null;
        trigger.Fired += (_, e) => received = e;
        trigger.Start();

        trigger.SimulateFire();

        Assert.NotNull(received);
        Assert.Same(trigger, received!.Trigger);
    }

    [Fact]
    public void RaiseFired_PassesData()
    {
        using var trigger = new TestTrigger();
        TriggerFiredEventArgs? received = null;
        trigger.Fired += (_, e) => received = e;
        trigger.Start();

        var data = new Dictionary<string, object?> { ["key"] = "value" };
        trigger.SimulateFire(data);

        Assert.NotNull(received);
        Assert.Equal("value", received!.Data["key"]);
    }

    [Fact]
    public void RaiseFired_WhenNotRunning_DoesNotFire()
    {
        using var trigger = new TestTrigger();
        bool fired = false;
        trigger.Fired += (_, _) => fired = true;

        trigger.SimulateFire(); // not started

        Assert.False(fired);
    }

    [Fact]
    public void RaiseFired_MaxFireCount_StopsAfterLimit()
    {
        using var trigger = new TestTrigger();
        trigger.Configuration.MaxFireCount = 2;
        int fireCount = 0;
        trigger.Fired += (_, _) => fireCount++;
        trigger.Start();

        trigger.SimulateFire();
        trigger.SimulateFire();
        trigger.SimulateFire(); // should be ignored

        Assert.Equal(2, fireCount);
    }

    [Fact]
    public void Dispose_StopsTrigger()
    {
        var trigger = new TestTrigger();
        trigger.Start();

        trigger.Dispose();

        Assert.False(trigger.IsRunning);
    }

    [Fact]
    public void Start_AfterDispose_Throws()
    {
        var trigger = new TestTrigger();
        trigger.Dispose();

        Assert.Throws<ObjectDisposedException>(() => trigger.Start());
    }
}

file class TestTrigger : TriggerBase
{
    public int StartCount { get; private set; }

    public TestTrigger() : base(null)
    {
        Name = "Test Trigger";
    }

    protected override void OnStart() => StartCount++;
    protected override void OnStop() { }

    public void SimulateFire(Dictionary<string, object?>? data = null)
        => RaiseFired(data);
}
