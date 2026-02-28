namespace UniversalTasker.Core.Actions;

public interface IContainerAction : IAction
{
    IEnumerable<IAction> GetChildActions();
}
