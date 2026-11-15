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
    public bool Is(Type target) => Bases.Contains(target.FullName!);
}
