namespace Facts.Domain.Shared.Dll;

using Umpire.Architecture;

public sealed record DllType(
    TypeName Name,
    string Namespace,
    string? Outer,
    Source Source,
    DllTypeKind Kind,
    DllAccess Access,
    DllTypeTraits Traits,
    DllShape? Base) {
    public IReadOnlyList<DllTypeParameter> TypeParameters { get; init; } = [];
    public IReadOnlyList<DllShape> Interfaces { get; init; } = [];
    public IReadOnlyList<DllShape> AllInterfaces { get; init; } = [];
    public IReadOnlyList<TypeName> Bases { get; init; } = [];
    public IReadOnlyList<TypeName> BaseArguments { get; init; } = [];
    public IReadOnlyList<DllAttributeFact> Attributes { get; init; } = [];
    public IReadOnlyList<DllMember> Members { get; init; } = [];
    public IReadOnlyList<string> MutableMembers { get; init; } = [];
    public IReadOnlyList<DllUse> Uses { get; init; } = [];
}
