using Microsoft.Extensions.Logging;

namespace UniversalTasker.Core.Triggers;

[TriggerMetadata("timer", "Timer", "Fires at regular intervals")]
public class TimerTrigger : TriggerBase
{
    private Timer? _timer;

    /// <summary>
    /// Interval between trigger fires.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Optional start time. If set, the trigger won't fire until this time.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Whether to fire immediately when started, or wait for the first interval.
    /// </summary>
    public bool FireImmediately { get; set; } = false;

    public TimerTrigger() : this(null)
    {
    }

    public TimerTrigger(ILogger? logger) : base(logger)
    {
        Name = "Timer Trigger";
    }

    protected override void OnStart()
    {
        var dueTime = CalculateDueTime();

        Logger.LogInformation(
            "Timer trigger starting with interval {Interval}, first fire in {DueTime}",
            Interval, dueTime);

        _timer = new Timer(OnTimerElapsed, null, dueTime, Interval);
    }

    protected override void OnStop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private TimeSpan CalculateDueTime()
    {
        if (FireImmediately)
        {
            return TimeSpan.Zero;
        }

        if (StartTime.HasValue)
        {
            var delay = StartTime.Value - DateTime.Now;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return Interval;
    }

    private void OnTimerElapsed(object? state)
    {
        if (!IsRunning) return;

        var data = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.Now,
            ["interval"] = Interval
        };

        RaiseFired(data);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
            _timer = null;
        }
        base.Dispose(disposing);
    }
}
