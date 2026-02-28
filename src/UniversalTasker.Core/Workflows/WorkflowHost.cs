using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Execution;
using UniversalTasker.Core.Expressions;
using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Core.Workflows;

public enum WorkflowHostState
{
    Idle,
    Running,
    Stopped
}

public class WorkflowExecutionEventArgs : EventArgs
{
    public Workflow Workflow { get; }
    public ITrigger? Trigger { get; }
    public TriggerFiredEventArgs? TriggerEventArgs { get; }

    public WorkflowExecutionEventArgs(Workflow workflow, ITrigger? trigger = null, TriggerFiredEventArgs? triggerEventArgs = null)
    {
        Workflow = workflow;
        Trigger = trigger;
        TriggerEventArgs = triggerEventArgs;
    }
}

public class WorkflowHost : IDisposable
{
    private readonly ILogger _logger;
    private readonly TriggerManager _triggerManager;
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    private CancellationTokenSource? _hostCts;
    private bool _disposed;

    public Workflow? Workflow { get; private set; }
    public WorkflowHostState State { get; private set; } = WorkflowHostState.Idle;
    public bool IsExecuting { get; private set; }

    public event EventHandler<WorkflowExecutionEventArgs>? ExecutionStarting;
    public event EventHandler<ExecutionCompletedEventArgs>? ExecutionCompleted;
    public event EventHandler<TriggerFiredEventArgs>? TriggerFired;

    public WorkflowHost(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _triggerManager = new TriggerManager(logger);
        _triggerManager.TriggerFired += OnTriggerFired;
    }

    public void Load(Workflow workflow)
    {
        if (State == WorkflowHostState.Running)
        {
            throw new InvalidOperationException("Cannot load workflow while host is running");
        }

        Workflow = workflow;
        _triggerManager.ClearTriggers();

        foreach (var trigger in workflow.Triggers)
        {
            _triggerManager.AddTrigger(trigger);
        }

        _logger.LogInformation("Loaded workflow: {Name} ({TriggerCount} triggers, {ActionCount} actions)",
            workflow.Name, workflow.Triggers.Count, workflow.Actions.Count);
    }

    public void Start()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WorkflowHost));
        if (Workflow == null) throw new InvalidOperationException("No workflow loaded");
        if (State == WorkflowHostState.Running) return;

        _hostCts = new CancellationTokenSource();
        State = WorkflowHostState.Running;

        if (Workflow.Settings.EnableTriggersOnStart)
        {
            _triggerManager.StartAll();
        }

        _logger.LogInformation("Workflow host started: {Name}", Workflow.Name);
    }

    public void Stop()
    {
        if (State != WorkflowHostState.Running) return;

        _hostCts?.Cancel();
        _triggerManager.StopAll();
        State = WorkflowHostState.Stopped;

        _logger.LogInformation("Workflow host stopped");
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (Workflow == null) throw new InvalidOperationException("No workflow loaded");

        await ExecuteInternalAsync(null, null, cancellationToken);
    }

    private async void OnTriggerFired(object? sender, TriggerFiredEventArgs e)
    {
        if (Workflow == null || State != WorkflowHostState.Running) return;

        TriggerFired?.Invoke(this, e);

        // Check concurrent execution setting
        if (!Workflow.Settings.AllowConcurrentExecution && IsExecuting)
        {
            _logger.LogDebug("Ignoring trigger {Name} - concurrent execution not allowed", e.Trigger.Name);
            return;
        }

        try
        {
            await ExecuteInternalAsync(e.Trigger, e, _hostCts?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow from trigger {Name}", e.Trigger.Name);
        }
    }

    private async Task ExecuteInternalAsync(ITrigger? trigger, TriggerFiredEventArgs? triggerArgs, CancellationToken cancellationToken)
    {
        if (Workflow == null) return;

        var acquired = await _executionLock.WaitAsync(0, cancellationToken);
        if (!acquired && !Workflow.Settings.AllowConcurrentExecution)
        {
            _logger.LogDebug("Execution already in progress, skipping");
            return;
        }

        if (!acquired)
        {
            await _executionLock.WaitAsync(cancellationToken);
        }

        try
        {
            IsExecuting = true;
            ExecutionStarting?.Invoke(this, new WorkflowExecutionEventArgs(Workflow, trigger, triggerArgs));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Apply max execution time
            if (Workflow.Settings.MaxExecutionTimeSeconds > 0)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(Workflow.Settings.MaxExecutionTimeSeconds));
            }

            // Create execution context with workflow variables
            var variables = new Dictionary<string, object?>(Workflow.Variables);

            // Inject trigger data into variables
            if (triggerArgs != null)
            {
                variables["trigger_name"] = trigger?.Name;
                variables["trigger_fired_at"] = triggerArgs.FiredAt;
                foreach (var kvp in triggerArgs.Data)
                {
                    variables[$"trigger_{kvp.Key}"] = kvp.Value;
                }
            }

            var context = new Actions.ExecutionContext(cts.Token, variables, _logger);
            context.RegisterService<IExpressionEvaluator>(new ExpressionEvaluator());

            Exception? caughtException = null;
            ExecutionState finalState = ExecutionState.Completed;

            try
            {
                foreach (var action in Workflow.Actions)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await action.ExecuteAsync(context);
                }
            }
            catch (OperationCanceledException)
            {
                finalState = ExecutionState.Cancelled;
            }
            catch (Exception ex) when (Workflow.Settings.StopOnError)
            {
                finalState = ExecutionState.Failed;
                caughtException = ex;
                _logger.LogError(ex, "Workflow execution failed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Action failed but continuing (StopOnError=false)");
            }

            ExecutionCompleted?.Invoke(this, new ExecutionCompletedEventArgs(finalState, caughtException));
        }
        finally
        {
            IsExecuting = false;
            _executionLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _triggerManager.TriggerFired -= OnTriggerFired;
        _triggerManager.Dispose();
        _executionLock.Dispose();
        _hostCts?.Dispose();

        _disposed = true;
    }
}
