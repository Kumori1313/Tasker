using System.Collections.ObjectModel;
using System.Reflection;
using UniversalTasker.Core.Actions;
using UniversalTasker.Core.Plugins;

namespace UniversalTasker.UI.ViewModels;

/// <summary>
/// ViewModel for plugin actions that are not natively known to the UI.
/// Reflects on the action's public properties to provide a generic property editor.
/// </summary>
public partial class GenericPluginActionViewModel : ActionViewModel
{
    private readonly IAction _action;

    public override string DisplayName { get; }
    public override string Category { get; }

    public ObservableCollection<PropertyItem> Properties { get; } = new();

    public GenericPluginActionViewModel(IAction action)
    {
        _action = action;
        Name = action.Name;

        // Prefer IActionMetadata if the action implements it, then fall back to ActionMetadataAttribute
        if (action is IActionMetadata meta)
        {
            DisplayName = meta.DisplayName;
            Category = meta.Category;
        }
        else
        {
            var attr = action.GetType().GetCustomAttribute<ActionMetadataAttribute>();
            DisplayName = attr?.DisplayName ?? action.GetType().Name;
            Category = attr?.Category ?? "Plugin";
        }

        PopulateProperties();
    }

    private void PopulateProperties()
    {
        // Skip properties already shown in the Name field or defined on IAction/ActionBase
        var skip = new HashSet<string> { nameof(IAction.Name), nameof(IAction.Category) };

        foreach (var prop in _action.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite || skip.Contains(prop.Name))
                continue;

            if (!IsSupportedType(prop.PropertyType))
                continue;

            var currentValue = prop.GetValue(_action)?.ToString() ?? "";
            Properties.Add(new PropertyItem(prop.Name, prop, _action, currentValue));
        }
    }

    private static bool IsSupportedType(Type t) =>
        t == typeof(string) || t == typeof(int) || t == typeof(double) ||
        t == typeof(float) || t == typeof(long) || t == typeof(bool) || t.IsEnum;

    public override IAction ToAction()
    {
        // Properties are mutated in-place via PropertyItem.OnValueChanged
        _action.Name = Name;
        return _action;
    }
}
