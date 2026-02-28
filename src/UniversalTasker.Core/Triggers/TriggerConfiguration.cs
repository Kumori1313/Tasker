namespace UniversalTasker.Core.Triggers;

public class TriggerConfiguration
{
    /// <summary>
    /// Minimum time between trigger fires (debounce).
    /// If the trigger fires again within this window, the second fire is ignored.
    /// </summary>
    public TimeSpan DebounceTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Maximum frequency of trigger fires (throttle).
    /// Limits how often the trigger can fire within a time window.
    /// </summary>
    public TimeSpan ThrottleTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Whether the trigger should be active when the workflow starts.
    /// </summary>
    public bool EnabledOnStart { get; set; } = true;

    /// <summary>
    /// Maximum number of times this trigger can fire. 0 means unlimited.
    /// </summary>
    public int MaxFireCount { get; set; } = 0;
}
