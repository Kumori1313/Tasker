using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalTasker.Core.Actions;

namespace UniversalTasker.UI.ViewModels;

public partial class PropertyItem : ObservableObject
{
    private readonly PropertyInfo _property;
    private readonly IAction _target;

    public string Name { get; }

    [ObservableProperty]
    private string _value;

    public PropertyItem(string name, PropertyInfo property, IAction target, string currentValue)
    {
        Name = name;
        _property = property;
        _target = target;
        _value = currentValue;
    }

    partial void OnValueChanged(string value)
    {
        try
        {
            var propType = _property.PropertyType;
            object? converted = propType switch
            {
                _ when propType == typeof(int) => int.TryParse(value, out var i) ? i : null,
                _ when propType == typeof(double) => double.TryParse(value, out var d) ? d : null,
                _ when propType == typeof(float) => float.TryParse(value, out var f) ? f : null,
                _ when propType == typeof(long) => long.TryParse(value, out var l) ? l : null,
                _ when propType == typeof(bool) => bool.TryParse(value, out var b) ? b : null,
                _ when propType.IsEnum => Enum.TryParse(propType, value, ignoreCase: true, out var e) ? e : null,
                _ => value
            };

            if (converted != null)
                _property.SetValue(_target, converted);
        }
        catch
        {
            // Silently ignore invalid values — user is mid-edit
        }
    }
}
