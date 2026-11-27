namespace Facts.Domain.Shared.Dll;

public sealed record DllMember(
    string Name,
    DllMemberKind Kind,
    DllAccess Access,
    DllMemberTraits Traits,
    int Line,
    DllShape? Type) {
    public IReadOnlyList<DllParameter> Parameters { get; init; } = [];
    public IReadOnlyList<DllTypeParameter> TypeParameters { get; init; } = [];
    public IReadOnlyList<TypeName> Implements { get; init; } = [];
    public DllAccess? Get { get; init; }
    public DllAccess? Set { get; init; }
}
