using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Serialization;

public class TriggerConverter : JsonConverter<ITrigger>
{
    private const string TypeDiscriminator = "$type";
    private readonly TriggerTypeRegistry _registry;

    public TriggerConverter() : this(TriggerTypeRegistry.Default)
    {
    }

    public TriggerConverter(TriggerTypeRegistry registry)
    {
        _registry = registry;
    }

    public override ITrigger? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        var triggerType = _registry.GetType(typeId);
        if (triggerType == null)
        {
            throw new JsonException($"Unknown trigger type: '{typeId}'");
        }

        return DeserializeTrigger(root, triggerType);
    }

    private ITrigger DeserializeTrigger(JsonElement element, Type triggerType)
    {
        var trigger = (ITrigger)Activator.CreateInstance(triggerType)!;

        if (element.TryGetProperty("name", out var nameElement))
        {
            trigger.Name = nameElement.GetString() ?? string.Empty;
        }

        if (element.TryGetProperty("isEnabled", out var enabledElement))
        {
            trigger.IsEnabled = enabledElement.GetBoolean();
        }

        // Deserialize configuration
        if (element.TryGetProperty("configuration", out var configElement))
        {
            DeserializeConfiguration(configElement, trigger.Configuration);
        }

        // Deserialize type-specific properties
        switch (trigger)
        {
            case TimerTrigger tt:
                DeserializeTimerTrigger(element, tt);
                break;
            case FileSystemTrigger fst:
                DeserializeFileSystemTrigger(element, fst);
                break;
            case HotkeyTrigger ht:
                DeserializeHotkeyTrigger(element, ht);
                break;
            default:
                ReflectionDeserializeProperties(element, trigger);
                break;
        }

        return trigger;
    }

    private void DeserializeConfiguration(JsonElement element, TriggerConfiguration config)
    {
        if (element.TryGetProperty("debounceTimeMs", out var debounce))
        {
            config.DebounceTime = TimeSpan.FromMilliseconds(debounce.GetDouble());
        }
        if (element.TryGetProperty("throttleTimeMs", out var throttle))
        {
            config.ThrottleTime = TimeSpan.FromMilliseconds(throttle.GetDouble());
        }
        if (element.TryGetProperty("enabledOnStart", out var enabled))
        {
            config.EnabledOnStart = enabled.GetBoolean();
        }
        if (element.TryGetProperty("maxFireCount", out var maxFire))
        {
            config.MaxFireCount = maxFire.GetInt32();
        }
    }

    private void DeserializeTimerTrigger(JsonElement element, TimerTrigger trigger)
    {
        if (element.TryGetProperty("interval", out var interval))
        {
            trigger.Interval = TimeSpan.Parse(interval.GetString() ?? "00:01:00");
        }
        if (element.TryGetProperty("startTime", out var startTime) && startTime.ValueKind != JsonValueKind.Null)
        {
            trigger.StartTime = DateTime.Parse(startTime.GetString()!);
        }
        if (element.TryGetProperty("fireImmediately", out var fireImm))
        {
            trigger.FireImmediately = fireImm.GetBoolean();
        }
    }

    private void DeserializeFileSystemTrigger(JsonElement element, FileSystemTrigger trigger)
    {
        if (element.TryGetProperty("path", out var path))
        {
            trigger.Path = path.GetString() ?? "";
        }
        if (element.TryGetProperty("filter", out var filter))
        {
            trigger.Filter = filter.GetString() ?? "*.*";
        }
        if (element.TryGetProperty("includeSubdirectories", out var subDirs))
        {
            trigger.IncludeSubdirectories = subDirs.GetBoolean();
        }
        if (element.TryGetProperty("watchCreated", out var created))
        {
            trigger.WatchCreated = created.GetBoolean();
        }
        if (element.TryGetProperty("watchChanged", out var changed))
        {
            trigger.WatchChanged = changed.GetBoolean();
        }
        if (element.TryGetProperty("watchDeleted", out var deleted))
        {
            trigger.WatchDeleted = deleted.GetBoolean();
        }
        if (element.TryGetProperty("watchRenamed", out var renamed))
        {
            trigger.WatchRenamed = renamed.GetBoolean();
        }
    }

    private void DeserializeHotkeyTrigger(JsonElement element, HotkeyTrigger trigger)
    {
        if (element.TryGetProperty("virtualKeyCode", out var vk))
        {
            trigger.VirtualKeyCode = vk.GetInt32();
        }
        if (element.TryGetProperty("ctrl", out var ctrl))
        {
            trigger.Ctrl = ctrl.GetBoolean();
        }
        if (element.TryGetProperty("alt", out var alt))
        {
            trigger.Alt = alt.GetBoolean();
        }
        if (element.TryGetProperty("shift", out var shift))
        {
            trigger.Shift = shift.GetBoolean();
        }
    }

    public override void Write(Utf8JsonWriter writer, ITrigger value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var typeId = _registry.GetTypeId(value.GetType());
        if (typeId == null)
        {
            throw new JsonException($"Trigger type {value.GetType().Name} is not registered");
        }

        writer.WriteString(TypeDiscriminator, typeId);
        writer.WriteString("name", value.Name);
        writer.WriteBoolean("isEnabled", value.IsEnabled);

        // Serialize configuration
        writer.WritePropertyName("configuration");
        SerializeConfiguration(writer, value.Configuration);

        // Serialize type-specific properties
        switch (value)
        {
            case TimerTrigger tt:
                SerializeTimerTrigger(writer, tt);
                break;
            case FileSystemTrigger fst:
                SerializeFileSystemTrigger(writer, fst);
                break;
            case HotkeyTrigger ht:
                SerializeHotkeyTrigger(writer, ht);
                break;
            default:
                ReflectionSerializeProperties(writer, value);
                break;
        }

        writer.WriteEndObject();
    }

    private void SerializeConfiguration(Utf8JsonWriter writer, TriggerConfiguration config)
    {
        writer.WriteStartObject();
        writer.WriteNumber("debounceTimeMs", config.DebounceTime.TotalMilliseconds);
        writer.WriteNumber("throttleTimeMs", config.ThrottleTime.TotalMilliseconds);
        writer.WriteBoolean("enabledOnStart", config.EnabledOnStart);
        writer.WriteNumber("maxFireCount", config.MaxFireCount);
        writer.WriteEndObject();
    }

    private void SerializeTimerTrigger(Utf8JsonWriter writer, TimerTrigger trigger)
    {
        writer.WriteString("interval", trigger.Interval.ToString());
        if (trigger.StartTime.HasValue)
        {
            writer.WriteString("startTime", trigger.StartTime.Value.ToString("O"));
        }
        else
        {
            writer.WriteNull("startTime");
        }
        writer.WriteBoolean("fireImmediately", trigger.FireImmediately);
    }

    private void SerializeFileSystemTrigger(Utf8JsonWriter writer, FileSystemTrigger trigger)
    {
        writer.WriteString("path", trigger.Path);
        writer.WriteString("filter", trigger.Filter);
        writer.WriteBoolean("includeSubdirectories", trigger.IncludeSubdirectories);
        writer.WriteBoolean("watchCreated", trigger.WatchCreated);
        writer.WriteBoolean("watchChanged", trigger.WatchChanged);
        writer.WriteBoolean("watchDeleted", trigger.WatchDeleted);
        writer.WriteBoolean("watchRenamed", trigger.WatchRenamed);
    }

    private void SerializeHotkeyTrigger(Utf8JsonWriter writer, HotkeyTrigger trigger)
    {
        writer.WriteNumber("virtualKeyCode", trigger.VirtualKeyCode);
        writer.WriteBoolean("ctrl", trigger.Ctrl);
        writer.WriteBoolean("alt", trigger.Alt);
        writer.WriteBoolean("shift", trigger.Shift);
    }

    private static readonly HashSet<string> TriggerBaseProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "IsEnabled", "IsRunning", "Configuration"
    };

    private void ReflectionSerializeProperties(Utf8JsonWriter writer, ITrigger trigger)
    {
        var properties = trigger.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !TriggerBaseProperties.Contains(p.Name));

        foreach (var prop in properties)
        {
            var jsonName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
            var value = prop.GetValue(trigger);
            ActionConverter.ReflectionWriteProperty(writer, jsonName, value, prop.PropertyType);
        }
    }

    private void ReflectionDeserializeProperties(JsonElement element, ITrigger trigger)
    {
        var properties = trigger.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !TriggerBaseProperties.Contains(p.Name));

        foreach (var prop in properties)
        {
            var jsonName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

            if (element.TryGetProperty(jsonName, out var propElement))
            {
                var value = ActionConverter.ReflectionReadValue(propElement, prop.PropertyType);
                if (value != null || !prop.PropertyType.IsValueType)
                {
                    prop.SetValue(trigger, value);
                }
            }
        }
    }
}
