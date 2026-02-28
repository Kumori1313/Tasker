using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Core.Workflows;

public class Workflow
{
    /// <summary>
    /// Unique identifier for the workflow.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name of the workflow.
    /// </summary>
    public string Name { get; set; } = "New Workflow";

    /// <summary>
    /// Optional description.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Workflow version for tracking changes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Workflow settings.
    /// </summary>
    public WorkflowSettings Settings { get; set; } = new();

    /// <summary>
    /// Initial variables available when the workflow starts.
    /// </summary>
    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>
    /// Triggers that can start the workflow.
    /// </summary>
    public List<ITrigger> Triggers { get; set; } = new();

    /// <summary>
    /// Actions to execute when the workflow runs.
    /// </summary>
    public List<IAction> Actions { get; set; } = new();

    /// <summary>
    /// When this workflow was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this workflow was last modified.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
