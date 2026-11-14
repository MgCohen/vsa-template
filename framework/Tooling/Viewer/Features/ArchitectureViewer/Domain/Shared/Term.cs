namespace ArchitectureViewer.Domain.Shared;

public sealed record Term(string Layer, IReadOnlyList<Condition> Conditions);
