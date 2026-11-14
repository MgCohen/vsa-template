using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Layouts;

public static class Shaping
{
    public static LayoutData Apply(LayoutData data, INodeShape shape)
    {
        List<Node> nodes = [.. data.Nodes.Select(shape.Shape)];
        Dictionary<string, Node> byId = nodes.ToDictionary(n => n.Id);
        List<Edge> edges = [.. data.Edges.GroupBy(e => Key(e, byId)).Select(Merge)];
        return data with { Nodes = nodes, Edges = edges };
    }

    public static Task<LayoutData> Draw(this ILayoutEngine engine, LayoutData data, INodeShape shape)
    {
        return engine.Arrange(Apply(data, shape));
    }

    public static bool Fits(LayoutData data, INodeShape shape)
    {
        LayoutData shaped = Apply(data, shape);
        bool sameSizes = shaped.Nodes.Zip(data.Nodes).All(p => p.First.W == p.Second.W && p.First.H == p.Second.H);
        return sameSizes && shaped.Edges.Count == data.Edges.Count;
    }

    public static (string, string, string, string) Key(Edge edge, IReadOnlyDictionary<string, Node> nodes)
    {
        Node from = nodes[edge.FromNode];
        Node to = nodes[edge.ToNode];
        return (edge.FromNode, Ports.Out(from, edge.FromRow).Id, edge.ToNode, Ports.In(to, edge.ToRow).Id);
    }

    private static Edge Merge(IEnumerable<Edge> edges)
    {
        List<Edge> all = [.. edges];
        if (all.Count == 1)
        {
            return all[0];
        }
        List<Edge> live = [.. all.Where(e => !e.Removed)];
        List<Edge> shown = live.Count == 0 ? all : live;
        List<string> labels = [.. shown.Select(e => e.Label).OfType<string>().Distinct()];
        return all[0] with
        {
            Access = AccessRules.Worst(shown.Select(e => e.Access)),
            Uses = [.. all.SelectMany(e => e.Uses)],
            Label = labels.Count == 0 ? null : string.Join("; ", labels),
            Pending = all.Any(e => e.Pending),
            Removed = live.Count == 0,
        };
    }
}
