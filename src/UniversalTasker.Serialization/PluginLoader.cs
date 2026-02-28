using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Plugins;
using UniversalTasker.Core.Triggers;

namespace UniversalTasker.Serialization;

public record PluginInfo(
    string Name,
    string Version,
    string Author,
    string Description,
    string AssemblyPath,
    List<string> ActionTypeIds,
    List<string> TriggerTypeIds);

public class PluginLoader
{
    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public List<PluginInfo> LoadPlugins(
        string pluginsDirectory,
        ActionTypeRegistry actionRegistry,
        TriggerTypeRegistry triggerRegistry)
    {
        var results = new List<PluginInfo>();

        if (!Directory.Exists(pluginsDirectory))
        {
            return results;
        }

        foreach (var pluginDir in Directory.GetDirectories(pluginsDirectory))
        {
            try
            {
                var info = LoadPlugin(pluginDir, actionRegistry, triggerRegistry);
                if (info != null)
                {
                    results.Add(info);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading plugin from '{Path.GetFileName(pluginDir)}': {ex.Message}");
            }
        }

        return results;
    }

    private PluginInfo? LoadPlugin(
        string pluginDir,
        ActionTypeRegistry actionRegistry,
        TriggerTypeRegistry triggerRegistry)
    {
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var manifest = ParseManifest(manifestPath);
        if (manifest == null || string.IsNullOrEmpty(manifest.Assembly))
        {
            return null;
        }

        var assemblyPath = Path.Combine(pluginDir, manifest.Assembly);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found: {manifest.Assembly}");
        }

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(
            Path.GetFullPath(assemblyPath));

        var actionTypeIds = new List<string>();
        var triggerTypeIds = new List<string>();

        foreach (var type in assembly.GetExportedTypes())
        {
            // Register actions
            if (typeof(IAction).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            {
                var actionMeta = type.GetCustomAttribute<ActionMetadataAttribute>();
                if (actionMeta != null)
                {
                    actionRegistry.Register(type);
                    actionTypeIds.Add(actionMeta.TypeId);
                }
            }

            // Register triggers
            if (typeof(ITrigger).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            {
                var triggerMeta = type.GetCustomAttribute<TriggerMetadataAttribute>();
                if (triggerMeta != null)
                {
                    triggerRegistry.Register(type);
                    triggerTypeIds.Add(triggerMeta.TypeId);
                }
            }

            // Initialize plugin entry points
            if (typeof(IActionPlugin).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
            {
                var plugin = (IActionPlugin)Activator.CreateInstance(type)!;
                plugin.Initialize();
            }
        }

        return new PluginInfo(
            manifest.Name,
            manifest.Version,
            manifest.Author,
            manifest.Description,
            assemblyPath,
            actionTypeIds,
            triggerTypeIds);
    }

    public static PluginManifest? ParseManifest(string manifestPath)
    {
        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<PluginManifest>(json, ManifestOptions);
    }
}
