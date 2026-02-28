using Microsoft.Extensions.Logging;

namespace UniversalTasker.Core.Workflows;

public class WorkflowSettings
{
    /// <summary>
    /// Whether to stop execution when an action throws an error.
    /// </summary>
    public bool StopOnError { get; set; } = true;

    /// <summary>
    /// Maximum execution time in seconds. 0 means no limit.
    /// </summary>
    public int MaxExecutionTimeSeconds { get; set; } = 0;

    /// <summary>
    /// Minimum log level for workflow execution.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Whether to run triggers when the workflow starts.
    /// </summary>
    public bool EnableTriggersOnStart { get; set; } = true;

    /// <summary>
    /// Whether to allow concurrent executions when triggers fire.
    /// If false, new triggers are ignored while executing.
    /// </summary>
    public bool AllowConcurrentExecution { get; set; } = false;
}
