namespace UniversalTasker.Core.Plugins;

/// <summary>
/// Optional interface for plugin actions that provide their own display metadata.
/// Implement this on an action class to supply a custom DisplayName and Category
/// used by the generic plugin editor in the UI.
/// </summary>
public interface IActionMetadata
{
    string DisplayName { get; }
    string Category { get; }
}
