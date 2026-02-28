using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UniversalTasker.Core.Triggers;

public class TriggerManager : IDisposable
{
    private readonly List<ITrigger> _triggers = new();
    private readonly ILogger _logger;
    private bool _disposed;
    private bool _isRunning;

    public IReadOnlyList<ITrigger> Triggers => _triggers.AsReadOnly();
    public bool IsRunning => _isRunning;

    public event EventHandler<TriggerFiredEventArgs>? TriggerFired;

    public TriggerManager(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public void AddTrigger(ITrigger trigger)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TriggerManager));

        _triggers.Add(trigger);
        trigger.Fired += OnTriggerFired;

        _logger.LogInformation("Added trigger: {Name} ({Type})", trigger.Name, trigger.GetType().Name);

        // If manager is already running, start the trigger
        if (_isRunning && trigger.IsEnabled)
        {
            trigger.Start();
        }
    }

    public bool RemoveTrigger(ITrigger trigger)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TriggerManager));

        trigger.Fired -= OnTriggerFired;
        trigger.Stop();

        var removed = _triggers.Remove(trigger);
        if (removed)
        {
            _logger.LogInformation("Removed trigger: {Name}", trigger.Name);
        }

        return removed;
    }

    public void ClearTriggers()
    {
        foreach (var trigger in _triggers.ToList())
        {
            RemoveTrigger(trigger);
        }
    }

    public void StartAll()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TriggerManager));
        if (_isRunning) return;

        _logger.LogInformation("Starting all triggers ({Count} total)", _triggers.Count);
        _isRunning = true;

        foreach (var trigger in _triggers)
        {
            if (trigger.IsEnabled)
            {
                try
                {
                    trigger.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start trigger: {Name}", trigger.Name);
                }
            }
        }
    }

    public void StopAll()
    {
        if (!_isRunning) return;

        _logger.LogInformation("Stopping all triggers");

        foreach (var trigger in _triggers)
        {
            try
            {
                trigger.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop trigger: {Name}", trigger.Name);
            }
        }

        _isRunning = false;
    }

    private void OnTriggerFired(object? sender, TriggerFiredEventArgs e)
    {
        TriggerFired?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopAll();

        foreach (var trigger in _triggers)
        {
            trigger.Fired -= OnTriggerFired;
            trigger.Dispose();
        }

        _triggers.Clear();
        _disposed = true;
    }
}
