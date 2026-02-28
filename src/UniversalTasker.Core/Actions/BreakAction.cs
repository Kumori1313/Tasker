namespace UniversalTasker.Core.Actions;

[ActionMetadata("break", "Break", "Flow")]
public class BreakAction : ActionBase
{
    public override string Category => "Flow";

    public BreakAction()
    {
        Name = "Break";
    }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);
        throw new LoopBreakException();
    }
}
