using System.Reflection;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.Serialization;

public class ActionTypeRegistry
{
    private readonly Dictionary<string, Type> _typeIdToType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, string> _typeToTypeId = new();

    public static ActionTypeRegistry Default { get; } = CreateDefault();

    private static ActionTypeRegistry CreateDefault()
    {
        var registry = new ActionTypeRegistry();
        registry.RegisterCoreActions();
        return registry;
    }

    public void Register<T>() where T : IAction
    {
        Register(typeof(T));
    }

    public void Register(Type actionType)
    {
        var attribute = actionType.GetCustomAttribute<ActionMetadataAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException(
                $"Type {actionType.Name} does not have ActionMetadataAttribute",
                nameof(actionType));
        }

        Register(attribute.TypeId, actionType);
    }

    public void Register(string typeId, Type actionType)
    {
        if (string.IsNullOrWhiteSpace(typeId))
            throw new ArgumentException("TypeId cannot be null or empty", nameof(typeId));

        if (!typeof(IAction).IsAssignableFrom(actionType))
            throw new ArgumentException($"Type {actionType.Name} must implement IAction", nameof(actionType));

        _typeIdToType[typeId] = actionType;
        _typeToTypeId[actionType] = typeId;
    }

    public Type? GetType(string typeId)
    {
        return _typeIdToType.TryGetValue(typeId, out var type) ? type : null;
    }

    public string? GetTypeId(Type actionType)
    {
        return _typeToTypeId.TryGetValue(actionType, out var typeId) ? typeId : null;
    }

    public string? GetTypeId<T>() where T : IAction
    {
        return GetTypeId(typeof(T));
    }

    public IEnumerable<(string TypeId, Type Type, ActionMetadataAttribute Metadata)> GetAllRegisteredTypes()
    {
        foreach (var kvp in _typeIdToType)
        {
            var metadata = kvp.Value.GetCustomAttribute<ActionMetadataAttribute>();
            if (metadata != null)
            {
                yield return (kvp.Key, kvp.Value, metadata);
            }
        }
    }

    private void RegisterCoreActions()
    {
        // Register all core actions from UniversalTasker.Core
        Register<MouseClickAction>();
        Register<KeyPressAction>();
        Register<DelayAction>();
        Register<SetVariableAction>();
        Register<RepeatAction>();
        Register<WhileAction>();
        Register<BreakAction>();
        Register<ContinueAction>();
        Register<ConditionAction>();
        Register<SequenceAction>();
    }
}
