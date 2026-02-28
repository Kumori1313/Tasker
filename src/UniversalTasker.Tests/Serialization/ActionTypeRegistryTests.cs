using UniversalTasker.Core.Actions;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public class ActionTypeRegistryTests
{
    [Fact]
    public void Default_RegistersAllCoreActions()
    {
        var registry = ActionTypeRegistry.Default;

        Assert.NotNull(registry.GetType("mouseclick"));
        Assert.NotNull(registry.GetType("keypress"));
        Assert.NotNull(registry.GetType("delay"));
        Assert.NotNull(registry.GetType("setvariable"));
        Assert.NotNull(registry.GetType("repeat"));
        Assert.NotNull(registry.GetType("while"));
        Assert.NotNull(registry.GetType("break"));
        Assert.NotNull(registry.GetType("continue"));
        Assert.NotNull(registry.GetType("condition"));
        Assert.NotNull(registry.GetType("sequence"));
    }

    [Fact]
    public void GetType_CaseInsensitive()
    {
        var registry = ActionTypeRegistry.Default;

        Assert.NotNull(registry.GetType("Delay"));
        Assert.NotNull(registry.GetType("DELAY"));
        Assert.NotNull(registry.GetType("delay"));
    }

    [Fact]
    public void GetType_UnknownType_ReturnsNull()
    {
        var registry = ActionTypeRegistry.Default;

        Assert.Null(registry.GetType("nonexistent"));
    }

    [Fact]
    public void GetTypeId_ReturnsCorrectId()
    {
        var registry = ActionTypeRegistry.Default;

        Assert.Equal("delay", registry.GetTypeId<DelayAction>());
        Assert.Equal("mouseclick", registry.GetTypeId<MouseClickAction>());
    }

    [Fact]
    public void GetTypeId_UnregisteredType_ReturnsNull()
    {
        var registry = new ActionTypeRegistry();

        Assert.Null(registry.GetTypeId<DelayAction>());
    }

    [Fact]
    public void GetAllRegisteredTypes_Returns10CoreActions()
    {
        var registry = ActionTypeRegistry.Default;
        var allTypes = registry.GetAllRegisteredTypes().ToList();

        Assert.Equal(10, allTypes.Count);
    }

    [Fact]
    public void Register_DuplicateTypeId_OverwritesPrevious()
    {
        var registry = new ActionTypeRegistry();
        registry.Register<DelayAction>();
        registry.Register("delay", typeof(SequenceAction)); // overwrite

        Assert.Equal(typeof(SequenceAction), registry.GetType("delay"));
    }

    [Fact]
    public void Register_EmptyTypeId_Throws()
    {
        var registry = new ActionTypeRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register("", typeof(DelayAction)));
    }
}
