using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed record PartInstance(
    string Name, IReadOnlyList<IType> Types, IReadOnlyList<IType> Denied, IReadOnlyList<IType> Limited, IReadOnlyList<Type> OfTypes,
    IReadOnlyDictionary<IType, Access> Limits, IReadOnlySet<IType> Framework) {
    public override string ToString() => Name;
}
