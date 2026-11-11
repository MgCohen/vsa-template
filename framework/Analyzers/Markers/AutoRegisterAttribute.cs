namespace VSA.Analyzers.Markers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
public sealed class AutoRegisterAttribute : Attribute
{
    public AutoRegisterAttribute(Lifetime lifetime = Lifetime.Scoped)
    {
        Lifetime = lifetime;
    }

    public Lifetime Lifetime { get; }
}
