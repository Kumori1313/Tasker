using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public class ValidationTests
{
    private readonly WorkflowSerializer _serializer = new();

    [Fact]
    public void Validate_ValidWorkflow_IsValid()
    {
        var json = """
        {
            "name": "Test",
            "actions": [
                { "$type": "delay", "name": "Wait", "durationMs": 100 }
            ],
            "triggers": []
        }
        """;

        var result = _serializer.Validate(json);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingName_HasError()
    {
        var json = """
        {
            "actions": []
        }
        """;

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("name"));
    }

    [Fact]
    public void Validate_UnknownActionType_HasError()
    {
        var json = """
        {
            "name": "Test",
            "actions": [
                { "$type": "nonexistent", "name": "Bad" }
            ]
        }
        """;

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("nonexistent"));
    }

    [Fact]
    public void Validate_MissingTypeProperty_HasError()
    {
        var json = """
        {
            "name": "Test",
            "actions": [
                { "name": "No Type" }
            ]
        }
        """;

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("$type"));
    }

    [Fact]
    public void Validate_UnknownTriggerType_HasError()
    {
        var json = """
        {
            "name": "Test",
            "triggers": [
                { "$type": "badtrigger" }
            ]
        }
        """;

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("badtrigger"));
    }

    [Fact]
    public void Validate_InvalidJson_HasError()
    {
        var json = "not valid json {{{";

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("JSON parse error"));
    }

    [Fact]
    public void Validate_NestedActions_ValidatesRecursively()
    {
        var json = """
        {
            "name": "Test",
            "actions": [
                {
                    "$type": "repeat",
                    "name": "Loop",
                    "actions": [
                        { "$type": "unknown_nested" }
                    ]
                }
            ]
        }
        """;

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unknown_nested"));
    }

    [Fact]
    public void Validate_ConditionAction_ValidatesThenAndElseActions()
    {
        var json = """
        {
            "name": "Test",
            "actions": [
                {
                    "$type": "condition",
                    "name": "Check",
                    "thenActions": [
                        { "$type": "bad_then" }
                    ],
                    "elseActions": [
                        { "$type": "delay" }
                    ]
                }
            ]
        }
        """;

        var result = _serializer.Validate(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("bad_then"));
    }
}
