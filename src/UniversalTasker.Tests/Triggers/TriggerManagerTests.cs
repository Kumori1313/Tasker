using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Tests.Triggers;

public class TriggerManagerTests
{
    [Fact]
    public void AddTrigger_IncreasesTriggerCount()
    {
        using var manager = new TriggerManager();

        manager.AddTrigger(new TestTrigger("T1"));

        Assert.Single(manager.Triggers);
    }

    [Fact]
    public void RemoveTrigger_DecreasesTriggerCount()
    {
        using var manager = new TriggerManager();
        var trigger = new TestTrigger("T1");
        manager.AddTrigger(trigger);

        var removed = manager.RemoveTrigger(trigger);

        Assert.True(removed);
        Assert.Empty(manager.Triggers);
    }

    [Fact]
    public void StartAll_StartsAllEnabledTriggers()
    {
        using var manager = new TriggerManager();
        var t1 = new TestTrigger("T1");
        var t2 = new TestTrigger("T2") { IsEnabled = false };
        manager.AddTrigger(t1);
        manager.AddTrigger(t2);

        manager.StartAll();

        Assert.True(t1.IsRunning);
        Assert.False(t2.IsRunning);
    }

    [Fact]
    public void StopAll_StopsAllTriggers()
    {
        using var manager = new TriggerManager();
        var t1 = new TestTrigger("T1");
        var t2 = new TestTrigger("T2");
        manager.AddTrigger(t1);
        manager.AddTrigger(t2);
        manager.StartAll();

        manager.StopAll();

        Assert.False(t1.IsRunning);
        Assert.False(t2.IsRunning);
        Assert.False(manager.IsRunning);
    }

    [Fact]
    public void TriggerFired_PropagatesFromChild()
    {
        using var manager = new TriggerManager();
        var trigger = new TestTrigger("T1");
        manager.AddTrigger(trigger);
        manager.StartAll();

        TriggerFiredEventArgs? received = null;
        manager.TriggerFired += (_, e) => received = e;

        trigger.SimulateFire();

        Assert.NotNull(received);
        Assert.Same(trigger, received!.Trigger);
    }

    [Fact]
    public void ClearTriggers_RemovesAll()
    {
        using var manager = new TriggerManager();
        manager.AddTrigger(new TestTrigger("T1"));
        manager.AddTrigger(new TestTrigger("T2"));

        manager.ClearTriggers();

        Assert.Empty(manager.Triggers);
    }

    [Fact]
    public void AddTrigger_WhileRunning_StartsNewTrigger()
    {
        using var manager = new TriggerManager();
        manager.StartAll();

        var trigger = new TestTrigger("T1");
        manager.AddTrigger(trigger);

        Assert.True(trigger.IsRunning);
    }

    [Fact]
    public void Dispose_CleansUpAllTriggers()
    {
        var manager = new TriggerManager();
        var trigger = new TestTrigger("T1");
        manager.AddTrigger(trigger);
        manager.StartAll();

        manager.Dispose();

        Assert.False(trigger.IsRunning);
    }
}

file class TestTrigger : TriggerBase
{
    public TestTrigger(string name) : base(null)
    {
        Name = name;
    }

    protected override void OnStart() { }
    protected override void OnStop() { }

    public void SimulateFire(Dictionary<string, object?>? data = null)
        => RaiseFired(data);
}
