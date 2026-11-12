namespace ArchitectureViewer.Domain.Shared;

public sealed record Edge(
    string Id,
    string FromNode,
    string? FromRow,
    string ToNode,
    string? ToRow,
    string Access,
    IReadOnlyList<Use> Uses,
    IReadOnlyList<double[]> Route,
    string? Label = null,
    bool Pending = false,
    bool Removed = false);
