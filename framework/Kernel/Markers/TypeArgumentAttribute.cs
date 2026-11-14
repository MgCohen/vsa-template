namespace VSA.Analyzers.Markers;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Assembly,
    AllowMultiple = true,
    Inherited = true)]
public sealed class TypeArgumentAttribute : Attribute
{
    public TypeArgumentAttribute()
    {
    }

    public TypeArgumentAttribute(Type type)
    {
        Type = type;
    }

    public Type? Type { get; }
}
