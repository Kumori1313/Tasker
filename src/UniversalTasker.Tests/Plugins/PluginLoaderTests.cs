using System.Text.Json;
using UniversalTasker.Core.Plugins;
using UniversalTasker.Serialization;

namespace UniversalTasker.Tests.Plugins;

public class PluginLoaderTests
{
    [Fact]
    public void ParseManifest_ValidJson_ReturnsManifest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var manifestPath = Path.Combine(tempDir, "plugin.json");
            File.WriteAllText(manifestPath, """
            {
                "name": "TestPlugin",
                "version": "1.0.0",
                "author": "Test Author",
                "description": "A test plugin",
                "assembly": "TestPlugin.dll"
            }
            """);

            var manifest = PluginLoader.ParseManifest(manifestPath);

            Assert.NotNull(manifest);
            Assert.Equal("TestPlugin", manifest!.Name);
            Assert.Equal("1.0.0", manifest.Version);
            Assert.Equal("Test Author", manifest.Author);
            Assert.Equal("A test plugin", manifest.Description);
            Assert.Equal("TestPlugin.dll", manifest.Assembly);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadPlugins_EmptyDirectory_ReturnsEmptyList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var loader = new PluginLoader();
            var results = loader.LoadPlugins(tempDir, new ActionTypeRegistry(), new TriggerTypeRegistry());

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadPlugins_NonExistentDirectory_ReturnsEmptyList()
    {
        var loader = new PluginLoader();
        var results = loader.LoadPlugins(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            new ActionTypeRegistry(),
            new TriggerTypeRegistry());

        Assert.Empty(results);
    }

    [Fact]
    public void LoadPlugins_DirectoryWithoutManifest_SkipsPlugin()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var pluginDir = Path.Combine(tempDir, "myplugin");
        Directory.CreateDirectory(pluginDir);
        try
        {
            // Plugin directory exists but has no plugin.json
            File.WriteAllText(Path.Combine(pluginDir, "readme.txt"), "not a plugin");

            var loader = new PluginLoader();
            var results = loader.LoadPlugins(tempDir, new ActionTypeRegistry(), new TriggerTypeRegistry());

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadPlugins_MissingAssembly_ContinuesWithOtherPlugins()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var pluginDir = Path.Combine(tempDir, "badplugin");
        Directory.CreateDirectory(pluginDir);
        try
        {
            File.WriteAllText(Path.Combine(pluginDir, "plugin.json"), """
            {
                "name": "BadPlugin",
                "version": "1.0.0",
                "author": "Test",
                "description": "Missing DLL",
                "assembly": "NonExistent.dll"
            }
            """);

            var loader = new PluginLoader();
            var results = loader.LoadPlugins(tempDir, new ActionTypeRegistry(), new TriggerTypeRegistry());

            // Should not throw, just skip the bad plugin
            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void PluginManifest_DefaultValues_AreEmptyStrings()
    {
        var manifest = new PluginManifest();

        Assert.Equal(string.Empty, manifest.Name);
        Assert.Equal(string.Empty, manifest.Version);
        Assert.Equal(string.Empty, manifest.Author);
        Assert.Equal(string.Empty, manifest.Description);
        Assert.Equal(string.Empty, manifest.Assembly);
    }
}
