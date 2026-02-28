namespace UniversalTasker.Core.Actions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ActionMetadataAttribute : Attribute
{
    public new string TypeId { get; }
    public string DisplayName { get; }
    public string Category { get; }

    public ActionMetadataAttribute(string typeId, string displayName, string category)
    {
        TypeId = typeId;
        DisplayName = displayName;
        Category = category;
    }
}
