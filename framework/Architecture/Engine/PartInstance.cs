using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed record PartInstance(
    string Name, IReadOnlyList<IType> Types, IReadOnlyList<IType> Denied, IReadOnlyList<IType> Limited, IReadOnlyList<Type> OfTypes) {
    public override string ToString() => Name;
}
