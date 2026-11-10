namespace VSA.Analyzers.Markers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = true)]
public sealed class KeyAttribute : Attribute
{
}
