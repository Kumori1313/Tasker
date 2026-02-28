using UniversalTasker.Core.Triggers;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Serialization;

public class TriggerTypeRegistryTests
{
    [Fact]
    public void Default_RegistersAllCoreTriggers()
    {
        var registry = TriggerTypeRegistry.Default;

        Assert.NotNull(registry.GetType("timer"));
        Assert.NotNull(registry.GetType("filesystem"));
        Assert.NotNull(registry.GetType("hotkey"));
    }

    [Fact]
    public void GetTypeId_ReturnsCorrectId()
    {
        var registry = TriggerTypeRegistry.Default;

        Assert.Equal("timer", registry.GetTypeId<TimerTrigger>());
        Assert.Equal("filesystem", registry.GetTypeId<FileSystemTrigger>());
        Assert.Equal("hotkey", registry.GetTypeId<HotkeyTrigger>());
    }

    [Fact]
    public void GetAllRegisteredTypes_Returns3CoreTriggers()
    {
        var registry = TriggerTypeRegistry.Default;
        var allTypes = registry.GetAllRegisteredTypes().ToList();

        Assert.Equal(3, allTypes.Count);
    }
}
