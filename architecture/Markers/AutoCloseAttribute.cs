namespace Umpire.Markers;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Assembly,
    AllowMultiple = true,
    Inherited = true)]
public sealed class AutoCloseAttribute : Attribute
{
    public AutoCloseAttribute()
    {
    }

    public AutoCloseAttribute(Type type)
    {
        Type = type;
    }

    public Type? Type { get; }
}
