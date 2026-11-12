using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkGraph(
    string Id,
    IReadOnlyDictionary<string, string> LayoutOptions,
    IReadOnlyList<ElkNode> Children,
    IReadOnlyList<ElkEdge> Edges)
{
    public static ElkGraph Of(LayoutData layout)
    {
        Dictionary<string, Node> nodes = layout.Nodes.ToDictionary(n => n.Id);
        List<ElkEdge> edges = [.. layout.Edges.Select(e => EdgeOf(e, nodes))];
        return new("g", ElkOptions.Root, ElkNode.ChildrenOf(layout, null, Offset.Zero), edges);
    }

    private static ElkEdge EdgeOf(Edge edge, Dictionary<string, Node> nodes)
    {
        Node from = nodes[edge.FromNode];
        Node to = nodes[edge.ToNode];
        string source = ElkPort.IdOf(from, Ports.Out(from, edge.FromRow));
        return new(edge.Id, [source], [ElkPort.IdOf(to, Ports.In(to, edge.ToRow))]);
    }
}
