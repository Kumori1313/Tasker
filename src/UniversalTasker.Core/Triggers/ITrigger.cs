namespace UniversalTasker.Core.Triggers;

public interface ITrigger : IDisposable
{
    string Name { get; set; }
    bool IsEnabled { get; set; }
    bool IsRunning { get; }
    TriggerConfiguration Configuration { get; }

    event EventHandler<TriggerFiredEventArgs>? Fired;

    void Start();
    void Stop();
}
