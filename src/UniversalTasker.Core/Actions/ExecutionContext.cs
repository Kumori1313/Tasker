using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace UniversalTasker.Core.Actions;

public class ExecutionContext
{
    private readonly Dictionary<Type, object> _services = new();

    public CancellationToken CancellationToken { get; }
    public Dictionary<string, object?> Variables { get; }
    public ILogger Logger { get; }

    public ExecutionContext(
        CancellationToken cancellationToken = default,
        Dictionary<string, object?>? variables = null,
        ILogger? logger = null)
    {
        CancellationToken = cancellationToken;
        Variables = variables ?? new Dictionary<string, object?>();
        Logger = logger ?? NullLogger.Instance;
        InitializeBuiltInVariables();
    }

    public void RegisterService<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    public T? GetService<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
    }

    public T GetRequiredService<T>() where T : class
    {
        return GetService<T>() ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }

    private void InitializeBuiltInVariables()
    {
        Variables["timestamp"] = DateTime.Now;
    }
}
