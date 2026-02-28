using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UniversalTasker.Core.Triggers;

public abstract class TriggerBase : ITrigger
{
    private DateTime _lastFiredAt = DateTime.MinValue;
    private int _fireCount;
    private bool _disposed;
    protected readonly ILogger Logger;

    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsRunning { get; protected set; }
    public TriggerConfiguration Configuration { get; } = new();

    public event EventHandler<TriggerFiredEventArgs>? Fired;

    protected TriggerBase(ILogger? logger = null)
    {
        Logger = logger ?? NullLogger.Instance;
    }

    public void Start()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
        if (IsRunning) return;
        if (!IsEnabled) return;

        Logger.LogInformation("Starting trigger: {Name}", Name);
        IsRunning = true;
        OnStart();
    }

    public void Stop()
    {
        if (!IsRunning) return;

        Logger.LogInformation("Stopping trigger: {Name}", Name);
        OnStop();
        IsRunning = false;
    }

    protected abstract void OnStart();
    protected abstract void OnStop();

    protected void RaiseFired(Dictionary<string, object?>? data = null)
    {
        if (!IsEnabled || !IsRunning) return;

        var now = DateTime.UtcNow;

        // Check debounce
        if (Configuration.DebounceTime > TimeSpan.Zero)
        {
            var elapsed = now - _lastFiredAt;
            if (elapsed < Configuration.DebounceTime)
            {
                Logger.LogDebug("Trigger {Name} debounced (elapsed: {Elapsed}ms)", Name, elapsed.TotalMilliseconds);
                return;
            }
        }

        // Check throttle
        if (Configuration.ThrottleTime > TimeSpan.Zero)
        {
            var elapsed = now - _lastFiredAt;
            if (elapsed < Configuration.ThrottleTime)
            {
                Logger.LogDebug("Trigger {Name} throttled (elapsed: {Elapsed}ms)", Name, elapsed.TotalMilliseconds);
                return;
            }
        }

        // Check max fire count
        if (Configuration.MaxFireCount > 0 && _fireCount >= Configuration.MaxFireCount)
        {
            Logger.LogDebug("Trigger {Name} reached max fire count ({Max})", Name, Configuration.MaxFireCount);
            return;
        }

        _lastFiredAt = now;
        _fireCount++;

        Logger.LogInformation("Trigger fired: {Name} (count: {Count})", Name, _fireCount);
        Fired?.Invoke(this, new TriggerFiredEventArgs(this, data));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Stop();
        }

        _disposed = true;
    }
}
