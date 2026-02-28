namespace UniversalTasker.Core.Plugins;

public interface IActionPlugin
{
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string Description { get; }
    void Initialize();
}
