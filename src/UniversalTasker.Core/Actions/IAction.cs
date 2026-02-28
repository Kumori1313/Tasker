namespace UniversalTasker.Core.Actions;

public interface IAction
{
    string Name { get; set; }
    string Category { get; }
    Task ExecuteAsync(ExecutionContext context);
}
