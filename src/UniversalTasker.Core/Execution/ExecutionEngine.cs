using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;

namespace UniversalTasker.Core.Execution;

public enum ExecutionState
{
    Idle,
    Running,
    Paused,
    Completed,
    Cancelled,
    Failed
}

public class ActionExecutingEventArgs : EventArgs
{
    public IAction Action { get; }
    public int Index { get; }
    public int TotalCount { get; }

    public ActionExecutingEventArgs(IAction action, int index, int totalCount)
    {
        Action = action;
        Index = index;
        TotalCount = totalCount;
    }
}

public class ExecutionCompletedEventArgs : EventArgs
{
    public ExecutionState FinalState { get; }
    public Exception? Exception { get; }

    public ExecutionCompletedEventArgs(ExecutionState finalState, Exception? exception = null)
    {
        FinalState = finalState;
        Exception = exception;
    }
}

public class ExecutionEngine
{
    private CancellationTokenSource? _cts;
    private readonly ManualResetEventSlim _pauseEvent = new(true);
    private readonly ILogger _logger;

    public ExecutionState State { get; private set; } = ExecutionState.Idle;
    public int CurrentActionIndex { get; private set; }

    public event EventHandler? ExecutionStarted;
    public event EventHandler<ActionExecutingEventArgs>? ActionExecuting;
    public event EventHandler<ExecutionCompletedEventArgs>? ExecutionCompleted;

    public ExecutionEngine(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public async Task StartAsync(IReadOnlyList<IAction> actions)
    {
        if (State == ExecutionState.Running)
        {
            throw new InvalidOperationException("Execution is already running.");
        }

        _cts = new CancellationTokenSource();
        _pauseEvent.Set();
        State = ExecutionState.Running;
        CurrentActionIndex = 0;

        _logger.LogInformation("Starting execution with {Count} actions", actions.Count);
        ExecutionStarted?.Invoke(this, EventArgs.Empty);

        Exception? caughtException = null;

        try
        {
            var context = new Actions.ExecutionContext(_cts.Token, logger: _logger);
            context.RegisterService<IExpressionEvaluator>(new ExpressionEvaluator());

            for (int i = 0; i < actions.Count; i++)
            {
                _pauseEvent.Wait(_cts.Token);
                _cts.Token.ThrowIfCancellationRequested();

                CurrentActionIndex = i;
                var action = actions[i];

                _logger.LogDebug("Executing action {Index}/{Total}: {Name}",
                    i + 1, actions.Count, action.Name);

                ActionExecuting?.Invoke(this, new ActionExecutingEventArgs(action, i, actions.Count));

                await action.ExecuteAsync(context);
            }

            State = ExecutionState.Completed;
            _logger.LogInformation("Execution completed successfully");
        }
        catch (OperationCanceledException)
        {
            State = ExecutionState.Cancelled;
            _logger.LogInformation("Execution was cancelled");
        }
        catch (Exception ex)
        {
            State = ExecutionState.Failed;
            caughtException = ex;
            _logger.LogError(ex, "Execution failed with exception");
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            ExecutionCompleted?.Invoke(this, new ExecutionCompletedEventArgs(State, caughtException));
        }
    }

    public void Stop()
    {
        if (_cts is null || State != ExecutionState.Running && State != ExecutionState.Paused)
        {
            return;
        }

        _logger.LogInformation("Stopping execution");
        _pauseEvent.Set();
        _cts.Cancel();
    }

    public void Pause()
    {
        if (State != ExecutionState.Running)
        {
            return;
        }

        _logger.LogInformation("Pausing execution");
        _pauseEvent.Reset();
        State = ExecutionState.Paused;
    }

    public void Resume()
    {
        if (State != ExecutionState.Paused)
        {
            return;
        }

        _logger.LogInformation("Resuming execution");
        _pauseEvent.Set();
        State = ExecutionState.Running;
    }
}
