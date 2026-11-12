namespace ArchitectureViewer.Domain.Shared;

public sealed record Definition(
    string Name,
    IReadOnlyList<Part> Parts,
    IReadOnlyList<Rule> Rules,
    IReadOnlyList<Pair> Pairs)
{
    public static readonly Definition Empty = new("", [], [], []);
}
