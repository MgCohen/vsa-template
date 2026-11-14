namespace ArchitectureViewer.Domain.Shared;

public sealed record Rule(string Id, string Text, string Access, IReadOnlyList<Term> Froms, IReadOnlyList<Term> Tos);
