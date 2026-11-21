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
    public TypeKind Kind { get; init; } = TypeKind.Class;
    public IReadOnlyList<string> Attributes { get; init; } = [];

    public bool Is(Type target) => Bases.Contains(target.FullName!);
}
