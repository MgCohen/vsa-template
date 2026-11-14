namespace ArchitectureViewer.Domain.Shared;

public sealed record LayoutData(
    Size Size,
    IReadOnlyList<Group> Groups,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Edge> Edges);
