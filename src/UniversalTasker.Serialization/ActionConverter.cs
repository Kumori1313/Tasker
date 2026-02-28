using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Expressions;
using UniversalTasker.Core.Input;

namespace UniversalTasker.Serialization;

public class ActionConverter : JsonConverter<IAction>
{
    private const string TypeDiscriminator = "$type";
    private readonly ActionTypeRegistry _registry;

    public ActionConverter() : this(ActionTypeRegistry.Default)
    {
    }

    public ActionConverter(ActionTypeRegistry registry)
    {
        _registry = registry;
    }

    public override IAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(TypeDiscriminator, out var typeProperty))
        {
            throw new JsonException($"Missing required property '{TypeDiscriminator}'");
        }

        var typeId = typeProperty.GetString();
        if (string.IsNullOrEmpty(typeId))
        {
            throw new JsonException($"Property '{TypeDiscriminator}' cannot be null or empty");
        }

        var actionType = _registry.GetType(typeId);
        if (actionType == null)
        {
            throw new JsonException($"Unknown action type: '{typeId}'");
        }

        return DeserializeAction(root, actionType, options);
    }

    private IAction DeserializeAction(JsonElement element, Type actionType, JsonSerializerOptions options)
    {
        var action = (IAction)Activator.CreateInstance(actionType)!;

        if (element.TryGetProperty("name", out var nameElement))
        {
            action.Name = nameElement.GetString() ?? string.Empty;
        }

        // Deserialize type-specific properties
        switch (action)
        {
            case MouseClickAction mca:
                DeserializeMouseClick(element, mca);
                break;
            case KeyPressAction kpa:
                DeserializeKeyPress(element, kpa);
                break;
            case DelayAction da:
                DeserializeDelay(element, da);
                break;
            case SetVariableAction sva:
                DeserializeSetVariable(element, sva);
                break;
            case RepeatAction ra:
                DeserializeRepeat(element, ra, options);
                break;
            case WhileAction wa:
                DeserializeWhile(element, wa, options);
                break;
            case ConditionAction ca:
                DeserializeCondition(element, ca, options);
                break;
            case SequenceAction sa:
                DeserializeSequence(element, sa, options);
                break;
            // BreakAction and ContinueAction have no additional properties
            default:
                ReflectionDeserializeProperties(element, action);
                break;
        }

        return action;
    }

    private void DeserializeMouseClick(JsonElement element, MouseClickAction action)
    {
        if (element.TryGetProperty("x", out var x)) action.X = x.GetInt32();
        if (element.TryGetProperty("y", out var y)) action.Y = y.GetInt32();
        if (element.TryGetProperty("button", out var button))
        {
            action.Button = Enum.Parse<MouseButton>(button.GetString() ?? "Left", ignoreCase: true);
        }
        if (element.TryGetProperty("clickCount", out var count)) action.ClickCount = count.GetInt32();
    }

    private void DeserializeKeyPress(JsonElement element, KeyPressAction action)
    {
        if (element.TryGetProperty("virtualKeyCode", out var vk)) action.VirtualKeyCode = (ushort)vk.GetInt32();
        if (element.TryGetProperty("ctrl", out var ctrl)) action.Ctrl = ctrl.GetBoolean();
        if (element.TryGetProperty("alt", out var alt)) action.Alt = alt.GetBoolean();
        if (element.TryGetProperty("shift", out var shift)) action.Shift = shift.GetBoolean();
    }

    private void DeserializeDelay(JsonElement element, DelayAction action)
    {
        if (element.TryGetProperty("durationMs", out var duration)) action.DurationMs = duration.GetInt32();
    }

    private void DeserializeSetVariable(JsonElement element, SetVariableAction action)
    {
        if (element.TryGetProperty("variableName", out var name)) action.VariableName = name.GetString() ?? "";
        if (element.TryGetProperty("value", out var value)) action.Value = value.GetString() ?? "";
        if (element.TryGetProperty("evaluateAsExpression", out var eval)) action.EvaluateAsExpression = eval.GetBoolean();
    }

    private void DeserializeRepeat(JsonElement element, RepeatAction action, JsonSerializerOptions options)
    {
        if (element.TryGetProperty("repeatCount", out var count)) action.RepeatCount = count.GetInt32();
        if (element.TryGetProperty("counterVariable", out var counter)) action.CounterVariable = counter.GetString() ?? "i";
        if (element.TryGetProperty("actions", out var actions))
        {
            foreach (var actionElement in actions.EnumerateArray())
            {
                var childAction = DeserializeActionElement(actionElement, options);
                if (childAction != null) action.Actions.Add(childAction);
            }
        }
    }

    private void DeserializeWhile(JsonElement element, WhileAction action, JsonSerializerOptions options)
    {
        if (element.TryGetProperty("maxIterations", out var max)) action.MaxIterations = max.GetInt32();
        if (element.TryGetProperty("condition", out var condition))
        {
            action.Condition = DeserializeConditionObject(condition);
        }
        if (element.TryGetProperty("actions", out var actions))
        {
            foreach (var actionElement in actions.EnumerateArray())
            {
                var childAction = DeserializeActionElement(actionElement, options);
                if (childAction != null) action.Actions.Add(childAction);
            }
        }
    }

    private void DeserializeCondition(JsonElement element, ConditionAction action, JsonSerializerOptions options)
    {
        if (element.TryGetProperty("condition", out var condition))
        {
            action.Condition = DeserializeConditionObject(condition);
        }
        if (element.TryGetProperty("thenActions", out var thenActions))
        {
            foreach (var actionElement in thenActions.EnumerateArray())
            {
                var childAction = DeserializeActionElement(actionElement, options);
                if (childAction != null) action.ThenActions.Add(childAction);
            }
        }
        if (element.TryGetProperty("elseActions", out var elseActions))
        {
            foreach (var actionElement in elseActions.EnumerateArray())
            {
                var childAction = DeserializeActionElement(actionElement, options);
                if (childAction != null) action.ElseActions.Add(childAction);
            }
        }
    }

    private void DeserializeSequence(JsonElement element, SequenceAction action, JsonSerializerOptions options)
    {
        if (element.TryGetProperty("actions", out var actions))
        {
            foreach (var actionElement in actions.EnumerateArray())
            {
                var childAction = DeserializeActionElement(actionElement, options);
                if (childAction != null) action.Actions.Add(childAction);
            }
        }
    }

    private Condition DeserializeConditionObject(JsonElement element)
    {
        var condition = new Condition();
        if (element.TryGetProperty("leftOperand", out var left)) condition.LeftOperand = left.GetString() ?? "";
        if (element.TryGetProperty("operator", out var op))
        {
            condition.Operator = Enum.Parse<ComparisonOperator>(op.GetString() ?? "Equals", ignoreCase: true);
        }
        if (element.TryGetProperty("rightOperand", out var right)) condition.RightOperand = right.GetString() ?? "";
        if (element.TryGetProperty("logicalOp", out var logOp) && logOp.ValueKind != JsonValueKind.Null)
        {
            condition.LogicalOp = Enum.Parse<LogicalOperator>(logOp.GetString() ?? "And", ignoreCase: true);
        }
        if (element.TryGetProperty("nextCondition", out var next) && next.ValueKind != JsonValueKind.Null)
        {
            condition.NextCondition = DeserializeConditionObject(next);
        }
        return condition;
    }

    private IAction? DeserializeActionElement(JsonElement element, JsonSerializerOptions options)
    {
        if (!element.TryGetProperty(TypeDiscriminator, out var typeProperty))
        {
            return null;
        }

        var typeId = typeProperty.GetString();
        if (string.IsNullOrEmpty(typeId)) return null;

        var actionType = _registry.GetType(typeId);
        if (actionType == null) return null;

        return DeserializeAction(element, actionType, options);
    }

    public override void Write(Utf8JsonWriter writer, IAction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var typeId = _registry.GetTypeId(value.GetType());
        if (typeId == null)
        {
            throw new JsonException($"Action type {value.GetType().Name} is not registered");
        }

        writer.WriteString(TypeDiscriminator, typeId);
        writer.WriteString("name", value.Name);

        // Serialize type-specific properties
        switch (value)
        {
            case MouseClickAction mca:
                SerializeMouseClick(writer, mca);
                break;
            case KeyPressAction kpa:
                SerializeKeyPress(writer, kpa);
                break;
            case DelayAction da:
                SerializeDelay(writer, da);
                break;
            case SetVariableAction sva:
                SerializeSetVariable(writer, sva);
                break;
            case RepeatAction ra:
                SerializeRepeat(writer, ra, options);
                break;
            case WhileAction wa:
                SerializeWhile(writer, wa, options);
                break;
            case ConditionAction ca:
                SerializeCondition(writer, ca, options);
                break;
            case SequenceAction sa:
                SerializeSequence(writer, sa, options);
                break;
            default:
                ReflectionSerializeProperties(writer, value);
                break;
        }

        writer.WriteEndObject();
    }

    private void SerializeMouseClick(Utf8JsonWriter writer, MouseClickAction action)
    {
        writer.WriteNumber("x", action.X);
        writer.WriteNumber("y", action.Y);
        writer.WriteString("button", action.Button.ToString());
        writer.WriteNumber("clickCount", action.ClickCount);
    }

    private void SerializeKeyPress(Utf8JsonWriter writer, KeyPressAction action)
    {
        writer.WriteNumber("virtualKeyCode", action.VirtualKeyCode);
        writer.WriteBoolean("ctrl", action.Ctrl);
        writer.WriteBoolean("alt", action.Alt);
        writer.WriteBoolean("shift", action.Shift);
    }

    private void SerializeDelay(Utf8JsonWriter writer, DelayAction action)
    {
        writer.WriteNumber("durationMs", action.DurationMs);
    }

    private void SerializeSetVariable(Utf8JsonWriter writer, SetVariableAction action)
    {
        writer.WriteString("variableName", action.VariableName);
        writer.WriteString("value", action.Value);
        writer.WriteBoolean("evaluateAsExpression", action.EvaluateAsExpression);
    }

    private void SerializeRepeat(Utf8JsonWriter writer, RepeatAction action, JsonSerializerOptions options)
    {
        writer.WriteNumber("repeatCount", action.RepeatCount);
        writer.WriteString("counterVariable", action.CounterVariable);
        writer.WritePropertyName("actions");
        writer.WriteStartArray();
        foreach (var child in action.Actions)
        {
            Write(writer, child, options);
        }
        writer.WriteEndArray();
    }

    private void SerializeWhile(Utf8JsonWriter writer, WhileAction action, JsonSerializerOptions options)
    {
        writer.WriteNumber("maxIterations", action.MaxIterations);
        writer.WritePropertyName("condition");
        SerializeConditionObject(writer, action.Condition);
        writer.WritePropertyName("actions");
        writer.WriteStartArray();
        foreach (var child in action.Actions)
        {
            Write(writer, child, options);
        }
        writer.WriteEndArray();
    }

    private void SerializeCondition(Utf8JsonWriter writer, ConditionAction action, JsonSerializerOptions options)
    {
        writer.WritePropertyName("condition");
        SerializeConditionObject(writer, action.Condition);
        writer.WritePropertyName("thenActions");
        writer.WriteStartArray();
        foreach (var child in action.ThenActions)
        {
            Write(writer, child, options);
        }
        writer.WriteEndArray();
        writer.WritePropertyName("elseActions");
        writer.WriteStartArray();
        foreach (var child in action.ElseActions)
        {
            Write(writer, child, options);
        }
        writer.WriteEndArray();
    }

    private void SerializeSequence(Utf8JsonWriter writer, SequenceAction action, JsonSerializerOptions options)
    {
        writer.WritePropertyName("actions");
        writer.WriteStartArray();
        foreach (var child in action.Actions)
        {
            Write(writer, child, options);
        }
        writer.WriteEndArray();
    }

    private static readonly HashSet<string> ActionBaseProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "Category"
    };

    private void ReflectionSerializeProperties(Utf8JsonWriter writer, IAction action)
    {
        var properties = action.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !ActionBaseProperties.Contains(p.Name));

        foreach (var prop in properties)
        {
            var jsonName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
            var value = prop.GetValue(action);

            ReflectionWriteProperty(writer, jsonName, value, prop.PropertyType);
        }
    }

    private void ReflectionDeserializeProperties(JsonElement element, IAction action)
    {
        var properties = action.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !ActionBaseProperties.Contains(p.Name));

        foreach (var prop in properties)
        {
            var jsonName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

            if (element.TryGetProperty(jsonName, out var propElement))
            {
                var value = ReflectionReadValue(propElement, prop.PropertyType);
                if (value != null || !prop.PropertyType.IsValueType)
                {
                    prop.SetValue(action, value);
                }
            }
        }
    }

    internal static void ReflectionWriteProperty(Utf8JsonWriter writer, string name, object? value, Type propertyType)
    {
        if (value == null)
        {
            writer.WriteNull(name);
            return;
        }

        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (type == typeof(string))
            writer.WriteString(name, (string)value);
        else if (type == typeof(int))
            writer.WriteNumber(name, (int)value);
        else if (type == typeof(long))
            writer.WriteNumber(name, (long)value);
        else if (type == typeof(double))
            writer.WriteNumber(name, (double)value);
        else if (type == typeof(float))
            writer.WriteNumber(name, (float)value);
        else if (type == typeof(bool))
            writer.WriteBoolean(name, (bool)value);
        else if (type == typeof(ushort))
            writer.WriteNumber(name, (ushort)value);
        else if (type.IsEnum)
            writer.WriteString(name, value.ToString());
        else if (type == typeof(TimeSpan))
            writer.WriteString(name, ((TimeSpan)value).ToString());
        else
            writer.WriteString(name, value.ToString());
    }

    internal static object? ReflectionReadValue(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (type == typeof(string))
            return element.GetString();
        if (type == typeof(int))
            return element.GetInt32();
        if (type == typeof(long))
            return element.GetInt64();
        if (type == typeof(double))
            return element.GetDouble();
        if (type == typeof(float))
            return element.GetSingle();
        if (type == typeof(bool))
            return element.GetBoolean();
        if (type == typeof(ushort))
            return (ushort)element.GetInt32();
        if (type.IsEnum)
            return Enum.Parse(type, element.GetString() ?? "", ignoreCase: true);
        if (type == typeof(TimeSpan))
            return TimeSpan.Parse(element.GetString() ?? "00:00:00");

        return element.GetString();
    }

    private void SerializeConditionObject(Utf8JsonWriter writer, Condition condition)
    {
        writer.WriteStartObject();
        writer.WriteString("leftOperand", condition.LeftOperand);
        writer.WriteString("operator", condition.Operator.ToString());
        writer.WriteString("rightOperand", condition.RightOperand);
        if (condition.LogicalOp.HasValue)
        {
            writer.WriteString("logicalOp", condition.LogicalOp.Value.ToString());
        }
        else
        {
            writer.WriteNull("logicalOp");
        }
        if (condition.NextCondition != null)
        {
            writer.WritePropertyName("nextCondition");
            SerializeConditionObject(writer, condition.NextCondition);
        }
        else
        {
            writer.WriteNull("nextCondition");
        }
        writer.WriteEndObject();
    }
}
