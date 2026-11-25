namespace Umpire.Architecture;

public sealed record TypeFacts(
    string FullName,
    string Name,
    string Assembly,
    Source? Source,
    IReadOnlyList<string> Bases,
    IReadOnlyList<string> Uses,
    IReadOnlyList<string> Mentions,
    IReadOnlyList<TypeExposure> Exposures) {
    public IReadOnlyList<string> BaseArguments { get; init; } = [];
    public bool IsAbstract { get; init; }
    public bool IsStatic { get; init; }
    public bool IsSealed { get; init; }
    public bool IsRecord { get; init; }
    public bool IsStruct { get; init; }
    public bool IsInterface { get; init; }
    public bool IsEnum { get; init; }
    public bool IsDelegate { get; init; }
    public IReadOnlyList<string> Attributes { get; init; } = [];
    public IReadOnlyList<string> References { get; init; } = [];
    public IReadOnlyList<string> MutableMembers { get; init; } = [];
    public bool IsNested { get; init; }

    public bool Is(Type target) => Bases.Contains(target.FullName!);
}
