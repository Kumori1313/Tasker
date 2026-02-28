using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Triggers;
using UniversalTasker.Core.Workflows;

namespace UniversalTasker.Serialization;

public class WorkflowSerializer
{
    private readonly ActionTypeRegistry _actionRegistry;
    private readonly TriggerTypeRegistry _triggerRegistry;
    private readonly JsonSerializerOptions _options;

    public WorkflowSerializer()
        : this(ActionTypeRegistry.Default, TriggerTypeRegistry.Default)
    {
    }

    public WorkflowSerializer(ActionTypeRegistry actionRegistry, TriggerTypeRegistry triggerRegistry)
    {
        _actionRegistry = actionRegistry;
        _triggerRegistry = triggerRegistry;

        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ActionConverter(actionRegistry),
                new TriggerConverter(triggerRegistry)
            }
        };
    }

    public string Serialize(Workflow workflow)
    {
        var json = new JsonObject
        {
            ["id"] = workflow.Id,
            ["name"] = workflow.Name,
            ["description"] = workflow.Description,
            ["version"] = workflow.Version,
            ["createdAt"] = workflow.CreatedAt.ToString("O"),
            ["modifiedAt"] = workflow.ModifiedAt.ToString("O"),
            ["settings"] = SerializeSettings(workflow.Settings),
            ["variables"] = SerializeVariables(workflow.Variables),
            ["triggers"] = SerializeTriggers(workflow.Triggers),
            ["actions"] = SerializeActions(workflow.Actions)
        };

        return json.ToJsonString(_options);
    }

    public Workflow Deserialize(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var workflow = new Workflow();

        if (root.TryGetProperty("id", out var id))
            workflow.Id = id.GetString() ?? Guid.NewGuid().ToString();

        if (root.TryGetProperty("name", out var name))
            workflow.Name = name.GetString() ?? "Untitled";

        if (root.TryGetProperty("description", out var desc))
            workflow.Description = desc.GetString() ?? "";

        if (root.TryGetProperty("version", out var version))
            workflow.Version = version.GetInt32();

        if (root.TryGetProperty("createdAt", out var created))
            workflow.CreatedAt = DateTime.Parse(created.GetString()!, null, DateTimeStyles.RoundtripKind);

        if (root.TryGetProperty("modifiedAt", out var modified))
            workflow.ModifiedAt = DateTime.Parse(modified.GetString()!, null, DateTimeStyles.RoundtripKind);

        if (root.TryGetProperty("settings", out var settings))
            workflow.Settings = DeserializeSettings(settings);

        if (root.TryGetProperty("variables", out var variables))
            workflow.Variables = DeserializeVariables(variables);

        if (root.TryGetProperty("triggers", out var triggers))
            workflow.Triggers = DeserializeTriggers(triggers);

        if (root.TryGetProperty("actions", out var actions))
            workflow.Actions = DeserializeActions(actions);

        return workflow;
    }

    public async Task SaveAsync(Workflow workflow, string filePath)
    {
        var json = Serialize(workflow);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<Workflow> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        return Deserialize(json);
    }

    public ValidationResult Validate(string json)
    {
        var result = new ValidationResult();

        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check required properties
            if (!root.TryGetProperty("name", out _))
            {
                result.Errors.Add("Missing required property: 'name'");
            }

            // Validate triggers
            if (root.TryGetProperty("triggers", out var triggers))
            {
                ValidateTriggers(triggers, result);
            }

            // Validate actions
            if (root.TryGetProperty("actions", out var actions))
            {
                ValidateActions(actions, result);
            }
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
        }

        return result;
    }

    private void ValidateTriggers(JsonElement triggers, ValidationResult result)
    {
        if (triggers.ValueKind != JsonValueKind.Array)
        {
            result.Errors.Add("'triggers' must be an array");
            return;
        }

        int index = 0;
        foreach (var trigger in triggers.EnumerateArray())
        {
            if (!trigger.TryGetProperty("$type", out var typeElement))
            {
                result.Errors.Add($"Trigger at index {index} missing '$type' property");
            }
            else
            {
                var typeId = typeElement.GetString();
                if (string.IsNullOrEmpty(typeId) || _triggerRegistry.GetType(typeId) == null)
                {
                    result.Errors.Add($"Unknown trigger type at index {index}: '{typeId}'");
                }
            }
            index++;
        }
    }

    private void ValidateActions(JsonElement actions, ValidationResult result)
    {
        if (actions.ValueKind != JsonValueKind.Array)
        {
            result.Errors.Add("'actions' must be an array");
            return;
        }

        ValidateActionArray(actions, result, "actions");
    }

    private void ValidateActionArray(JsonElement actions, ValidationResult result, string path)
    {
        int index = 0;
        foreach (var action in actions.EnumerateArray())
        {
            var currentPath = $"{path}[{index}]";

            if (!action.TryGetProperty("$type", out var typeElement))
            {
                result.Errors.Add($"Action at {currentPath} missing '$type' property");
            }
            else
            {
                var typeId = typeElement.GetString();
                if (string.IsNullOrEmpty(typeId) || _actionRegistry.GetType(typeId) == null)
                {
                    result.Errors.Add($"Unknown action type at {currentPath}: '{typeId}'");
                }
            }

            // Recursively validate nested actions
            if (action.TryGetProperty("actions", out var nestedActions))
            {
                ValidateActionArray(nestedActions, result, $"{currentPath}.actions");
            }
            if (action.TryGetProperty("thenActions", out var thenActions))
            {
                ValidateActionArray(thenActions, result, $"{currentPath}.thenActions");
            }
            if (action.TryGetProperty("elseActions", out var elseActions))
            {
                ValidateActionArray(elseActions, result, $"{currentPath}.elseActions");
            }

            index++;
        }
    }

    private JsonObject SerializeSettings(WorkflowSettings settings)
    {
        return new JsonObject
        {
            ["stopOnError"] = settings.StopOnError,
            ["maxExecutionTimeSeconds"] = settings.MaxExecutionTimeSeconds,
            ["logLevel"] = settings.LogLevel.ToString(),
            ["enableTriggersOnStart"] = settings.EnableTriggersOnStart,
            ["allowConcurrentExecution"] = settings.AllowConcurrentExecution
        };
    }

    private WorkflowSettings DeserializeSettings(JsonElement element)
    {
        var settings = new WorkflowSettings();

        if (element.TryGetProperty("stopOnError", out var stopOnError))
            settings.StopOnError = stopOnError.GetBoolean();

        if (element.TryGetProperty("maxExecutionTimeSeconds", out var maxTime))
            settings.MaxExecutionTimeSeconds = maxTime.GetInt32();

        if (element.TryGetProperty("logLevel", out var logLevel))
            settings.LogLevel = Enum.Parse<LogLevel>(logLevel.GetString() ?? "Information", ignoreCase: true);

        if (element.TryGetProperty("enableTriggersOnStart", out var enableTriggers))
            settings.EnableTriggersOnStart = enableTriggers.GetBoolean();

        if (element.TryGetProperty("allowConcurrentExecution", out var concurrent))
            settings.AllowConcurrentExecution = concurrent.GetBoolean();

        return settings;
    }

    private JsonObject SerializeVariables(Dictionary<string, object?> variables)
    {
        var obj = new JsonObject();
        foreach (var kvp in variables)
        {
            obj[kvp.Key] = ToJsonNode(kvp.Value);
        }
        return obj;
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        return value switch
        {
            null => null,
            string s => JsonValue.Create(s),
            int i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            double d => JsonValue.Create(d),
            float f => JsonValue.Create(f),
            decimal dec => JsonValue.Create(dec),
            bool b => JsonValue.Create(b),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private Dictionary<string, object?> DeserializeVariables(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = GetJsonValue(prop.Value);
        }

        return dict;
    }

    private object? GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private JsonArray SerializeTriggers(List<ITrigger> triggers)
    {
        var array = new JsonArray();
        var converter = new TriggerConverter(_triggerRegistry);

        foreach (var trigger in triggers)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, trigger, _options);
            writer.Flush();

            stream.Position = 0;
            var node = JsonNode.Parse(stream);
            if (node != null) array.Add(node);
        }

        return array;
    }

    private List<ITrigger> DeserializeTriggers(JsonElement element)
    {
        var triggers = new List<ITrigger>();
        var converter = new TriggerConverter(_triggerRegistry);

        foreach (var triggerElement in element.EnumerateArray())
        {
            var json = triggerElement.GetRawText();
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
            reader.Read();
            var trigger = converter.Read(ref reader, typeof(ITrigger), _options);
            if (trigger != null) triggers.Add(trigger);
        }

        return triggers;
    }

    private JsonArray SerializeActions(List<IAction> actions)
    {
        var array = new JsonArray();
        var converter = new ActionConverter(_actionRegistry);

        foreach (var action in actions)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            converter.Write(writer, action, _options);
            writer.Flush();

            stream.Position = 0;
            var node = JsonNode.Parse(stream);
            if (node != null) array.Add(node);
        }

        return array;
    }

    private List<IAction> DeserializeActions(JsonElement element)
    {
        var actions = new List<IAction>();
        var converter = new ActionConverter(_actionRegistry);

        foreach (var actionElement in element.EnumerateArray())
        {
            var json = actionElement.GetRawText();
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
            reader.Read();
            var action = converter.Read(ref reader, typeof(IAction), _options);
            if (action != null) actions.Add(action);
        }

        return actions;
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;
}
