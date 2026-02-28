using System.Text.Json;
using UniversalTasker.Core.Actions;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public enum TestPriority
{
    Low,
    Medium,
    High
}

[ActionMetadata("test-custom", "Test Custom Action", "Testing")]
public class TestCustomAction : IAction
{
    public string Name { get; set; } = "Test Custom";
    public string Category => "Testing";

    public string Message { get; set; } = "";
    public int Count { get; set; }
    public bool IsActive { get; set; }
    public double Multiplier { get; set; }
    public TestPriority Priority { get; set; } = TestPriority.Medium;
    public ushort Port { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public TestCustomAction() { }

    public Task ExecuteAsync(ExecutionContext context) => Task.CompletedTask;
}

public class ReflectionSerializationTests
{
    private ActionTypeRegistry CreateRegistryWithTestAction()
    {
        var registry = new ActionTypeRegistry();
        registry.Register<TestCustomAction>();
        return registry;
    }

    [Fact]
    public void RoundTrip_CustomAction_PreservesAllProperties()
    {
        var registry = CreateRegistryWithTestAction();
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        var original = new TestCustomAction
        {
            Name = "My Test",
            Message = "Hello World",
            Count = 42,
            IsActive = true,
            Multiplier = 3.14,
            Priority = TestPriority.High,
            Port = 8080,
            Timeout = TimeSpan.FromMinutes(5)
        };

        var json = JsonSerializer.Serialize<IAction>(original, options);
        var deserialized = JsonSerializer.Deserialize<IAction>(json, options);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<TestCustomAction>(deserialized);
        Assert.Equal("My Test", result.Name);
        Assert.Equal("Hello World", result.Message);
        Assert.Equal(42, result.Count);
        Assert.True(result.IsActive);
        Assert.Equal(3.14, result.Multiplier);
        Assert.Equal(TestPriority.High, result.Priority);
        Assert.Equal((ushort)8080, result.Port);
        Assert.Equal(TimeSpan.FromMinutes(5), result.Timeout);
    }

    [Fact]
    public void Serialize_CustomAction_ContainsTypeDiscriminator()
    {
        var registry = CreateRegistryWithTestAction();
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        var action = new TestCustomAction { Name = "Test" };
        var json = JsonSerializer.Serialize<IAction>(action, options);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("$type", out var typeElement));
        Assert.Equal("test-custom", typeElement.GetString());
    }

    [Fact]
    public void Serialize_CustomAction_UseCamelCase()
    {
        var registry = CreateRegistryWithTestAction();
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        var action = new TestCustomAction
        {
            IsActive = true,
            Multiplier = 1.5
        };
        var json = JsonSerializer.Serialize<IAction>(action, options);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("isActive", out _));
        Assert.True(doc.RootElement.TryGetProperty("multiplier", out _));
    }

    [Fact]
    public void Deserialize_CustomAction_DefaultValues()
    {
        var registry = CreateRegistryWithTestAction();
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        var json = """{"$type":"test-custom","name":"Minimal"}""";
        var result = JsonSerializer.Deserialize<IAction>(json, options);

        Assert.NotNull(result);
        var action = Assert.IsType<TestCustomAction>(result);
        Assert.Equal("Minimal", action.Name);
        Assert.Equal("", action.Message);
        Assert.Equal(0, action.Count);
        Assert.False(action.IsActive);
        Assert.Equal(TestPriority.Medium, action.Priority);
    }

    [Fact]
    public void RoundTrip_CustomAction_Enum_CaseInsensitive()
    {
        var registry = CreateRegistryWithTestAction();
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        // Manually build JSON with lowercase enum value
        var json = """{"$type":"test-custom","name":"Test","priority":"low"}""";
        var result = JsonSerializer.Deserialize<IAction>(json, options);

        Assert.NotNull(result);
        var action = Assert.IsType<TestCustomAction>(result);
        Assert.Equal(TestPriority.Low, action.Priority);
    }

    [Fact]
    public void Serialize_ThenDeserialize_ThenSerializeAgain_ProducesSameJson()
    {
        var registry = CreateRegistryWithTestAction();
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        var original = new TestCustomAction
        {
            Name = "Stable",
            Message = "test",
            Count = 10,
            IsActive = true,
            Multiplier = 2.5,
            Priority = TestPriority.Low,
            Port = 443,
            Timeout = TimeSpan.FromSeconds(60)
        };

        var json1 = JsonSerializer.Serialize<IAction>(original, options);
        var deserialized = JsonSerializer.Deserialize<IAction>(json1, options)!;
        var json2 = JsonSerializer.Serialize<IAction>(deserialized, options);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void CoreActions_StillWorkWithDefaultCase()
    {
        // Ensure core actions still serialize/deserialize correctly
        // (they should hit their specific switch cases, not the default)
        var registry = ActionTypeRegistry.Default;
        var converter = new ActionConverter(registry);
        var options = new JsonSerializerOptions { Converters = { converter } };

        var delay = new DelayAction { Name = "Wait", DurationMs = 500 };
        var json = JsonSerializer.Serialize<IAction>(delay, options);
        var result = JsonSerializer.Deserialize<IAction>(json, options);

        Assert.NotNull(result);
        var deserialized = Assert.IsType<DelayAction>(result);
        Assert.Equal(500, deserialized.DurationMs);
    }
}
