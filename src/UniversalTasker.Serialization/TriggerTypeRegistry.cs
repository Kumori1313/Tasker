using System.Reflection;
using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Serialization;

public class TriggerTypeRegistry
{
    private readonly Dictionary<string, Type> _typeIdToType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, string> _typeToTypeId = new();

    public static TriggerTypeRegistry Default { get; } = CreateDefault();

    private static TriggerTypeRegistry CreateDefault()
    {
        var registry = new TriggerTypeRegistry();
        registry.RegisterCoreTriggers();
        return registry;
    }

    public void Register<T>() where T : ITrigger
    {
        Register(typeof(T));
    }

    public void Register(Type triggerType)
    {
        var attribute = triggerType.GetCustomAttribute<TriggerMetadataAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException(
                $"Type {triggerType.Name} does not have TriggerMetadataAttribute",
                nameof(triggerType));
        }

        Register(attribute.TypeId, triggerType);
    }

    public void Register(string typeId, Type triggerType)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            throw new ArgumentException("TypeId cannot be null or empty", nameof(typeId));

        if (!typeof(ITrigger).IsAssignableFrom(triggerType))
            throw new ArgumentException($"Type {triggerType.Name} must implement ITrigger", nameof(triggerType));

        _typeIdToType[typeId] = triggerType;
        _typeToTypeId[triggerType] = typeId;
    }

    public Type? GetType(string typeId)
    {
        return _typeIdToType.TryGetValue(typeId, out var type) ? type : null;
    }

    public string? GetTypeId(Type triggerType)
    {
        return _typeToTypeId.TryGetValue(triggerType, out var typeId) ? typeId : null;
    }

    public string? GetTypeId<T>() where T : ITrigger
    {
        return GetTypeId(typeof(T));
    }

    public IEnumerable<(string TypeId, Type Type, TriggerMetadataAttribute Metadata)> GetAllRegisteredTypes()
    {
        foreach (var kvp in _typeIdToType)
        {
            var metadata = kvp.Value.GetCustomAttribute<TriggerMetadataAttribute>();
            if (metadata != null)
            {
                yield return (kvp.Key, kvp.Value, metadata);
            }
        }
    }

    private void RegisterCoreTriggers()
    {
        Register<TimerTrigger>();
        Register<FileSystemTrigger>();
        Register<HotkeyTrigger>();
    }
}
