using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkNode(
    string Id,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyList<ElkPort> Ports,
    IReadOnlyList<ElkNode> Children,
    IReadOnlyDictionary<string, string> LayoutOptions)
{
    public static List<ElkNode> ChildrenOf(LayoutData layout, string? parent, Offset origin)
    {
        IEnumerable<ElkNode> groups = layout.Groups.Where(g => g.Parent == parent).Select(g => new ElkNode(
            g.Id,
            g.X - origin.X,
            g.Y - origin.Y,
            0,
            0,
            [],
            ChildrenOf(layout, g.Id, new Offset(g.X, g.Y)),
            ElkOptions.Group));
        IEnumerable<ElkNode> nodes = layout.Nodes.Where(n => n.Parent == parent).Select(n => new ElkNode(
            n.Id, n.X - origin.X, n.Y - origin.Y, n.W, n.H, ElkPort.Of(n), [], ElkOptions.Card));
        return [.. groups.Concat(nodes).OrderBy(n => n.Y).ThenBy(n => n.X)];
    }
}
