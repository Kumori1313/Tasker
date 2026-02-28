namespace UniversalTasker.Core.Triggers;

public class TriggerFiredEventArgs : EventArgs
{
    public ITrigger Trigger { get; }
    public DateTime FiredAt { get; }
    public Dictionary<string, object?> Data { get; }

    public TriggerFiredEventArgs(ITrigger trigger, Dictionary<string, object?>? data = null)
    {
        Trigger = trigger;
        FiredAt = DateTime.UtcNow;
        Data = data ?? new Dictionary<string, object?>();
    }
}
