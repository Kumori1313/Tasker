using System.Reflection;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.Tests.Actions;

public class ActionMetadataTests
{
    [Fact]
    public void AllActionBaseSubclasses_HaveActionMetadataAttribute()
    {
        var actionTypes = typeof(ActionBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ActionBase)) && !t.IsAbstract);

        foreach (var type in actionTypes)
        {
            var attr = type.GetCustomAttribute<ActionMetadataAttribute>();
            Assert.True(attr != null, $"{type.Name} is missing [ActionMetadata] attribute");
        }
    }

    [Fact]
    public void ActionMetadata_TypeIds_AreUnique()
    {
        var actionTypes = typeof(ActionBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ActionBase)) && !t.IsAbstract);

        var typeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in actionTypes)
        {
            var attr = type.GetCustomAttribute<ActionMetadataAttribute>();
            if (attr != null)
            {
                Assert.True(typeIds.Add(attr.TypeId),
                    $"Duplicate TypeId '{attr.TypeId}' found on {type.Name}");
            }
        }
    }

    [Theory]
    [InlineData(typeof(DelayAction), "delay", "Delay", "Flow")]
    [InlineData(typeof(MouseClickAction), "mouseclick", "Mouse Click", "Input")]
    [InlineData(typeof(KeyPressAction), "keypress", "Key Press", "Input")]
    [InlineData(typeof(SetVariableAction), "setvariable", "Set Variable", "Variables")]
    [InlineData(typeof(RepeatAction), "repeat", "Repeat", "Flow")]
    [InlineData(typeof(ConditionAction), "condition", "If/Else", "Flow")]
    [InlineData(typeof(SequenceAction), "sequence", "Sequence", "Flow")]
    [InlineData(typeof(BreakAction), "break", "Break", "Flow")]
    [InlineData(typeof(ContinueAction), "continue", "Continue", "Flow")]
    public void ActionMetadata_HasCorrectValues(Type actionType, string expectedTypeId, string expectedDisplay, string expectedCategory)
    {
        var attr = actionType.GetCustomAttribute<ActionMetadataAttribute>();

        Assert.NotNull(attr);
        Assert.Equal(expectedTypeId, attr!.TypeId);
        Assert.Equal(expectedDisplay, attr.DisplayName);
        Assert.Equal(expectedCategory, attr.Category);
    }
}
