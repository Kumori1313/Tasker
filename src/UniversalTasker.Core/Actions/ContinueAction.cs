namespace UniversalTasker.Core.Actions;

[ActionMetadata("continue", "Continue", "Flow")]
public class ContinueAction : ActionBase
{
    public override string Category => "Flow";

    public ContinueAction()
    {
        Name = "Continue";
    }

    public override Task ExecuteAsync(ExecutionContext context)
    {
        ThrowIfCancelled(context);
        throw new LoopContinueException();
    }
}
