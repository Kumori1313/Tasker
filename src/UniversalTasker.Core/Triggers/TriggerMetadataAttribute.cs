namespace UniversalTasker.Core.Triggers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TriggerMetadataAttribute : Attribute
{
    public new string TypeId { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public TriggerMetadataAttribute(string typeId, string displayName, string description = "")
    {
        TypeId = typeId;
        DisplayName = displayName;
        Description = description;
    }
}
