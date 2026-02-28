namespace UniversalTasker.Core.Actions;

public abstract class ActionBase : IAction
{
    public string Name { get; set; } = string.Empty;
    public abstract string Category { get; }

    public abstract Task ExecuteAsync(ExecutionContext context);

    protected void ThrowIfCancelled(ExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
    }
}
