using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Tests.Triggers;

public class TimerTriggerTests
{
    [Fact]
    public async Task TimerTrigger_FiresAtInterval()
    {
        using var trigger = new TimerTrigger
        {
            Interval = TimeSpan.FromMilliseconds(50),
            FireImmediately = true
        };

        int fireCount = 0;
        var tcs = new TaskCompletionSource();
        trigger.Fired += (_, _) =>
        {
            fireCount++;
            if (fireCount >= 2) tcs.TrySetResult();
        };

        trigger.Start();

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000));
        trigger.Stop();

        Assert.True(completed == tcs.Task, $"Timer only fired {fireCount} time(s) in 2 seconds");
        Assert.True(fireCount >= 2);
    }

    [Fact]
    public void TimerTrigger_DefaultInterval_IsOneMinute()
    {
        using var trigger = new TimerTrigger();
        Assert.Equal(TimeSpan.FromMinutes(1), trigger.Interval);
    }

    [Fact]
    public void TimerTrigger_Stop_PreventsMoreFires()
    {
        using var trigger = new TimerTrigger
        {
            Interval = TimeSpan.FromMilliseconds(50),
            FireImmediately = true
        };

        int fireCount = 0;
        trigger.Fired += (_, _) => fireCount++;

        trigger.Start();
        trigger.Stop();

        var countAfterStop = fireCount;
        Thread.Sleep(200);

        Assert.Equal(countAfterStop, fireCount);
    }

    [Fact]
    public void TimerTrigger_HasCorrectMetadata()
    {
        var attr = (TriggerMetadataAttribute?)Attribute.GetCustomAttribute(
            typeof(TimerTrigger), typeof(TriggerMetadataAttribute));

        Assert.NotNull(attr);
        Assert.Equal("timer", attr!.TypeId);
        Assert.Equal("Timer", attr.DisplayName);
    }
}
