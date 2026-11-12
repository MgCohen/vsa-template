using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkResult(
    double Width,
    double Height,
    IReadOnlyList<ElkShape> Children,
    IReadOnlyList<ElkRoute> Edges)
{
    public LayoutData ToLayout(LayoutData source)
    {
        ElkPlacement placement = new(source);
        foreach (ElkShape shape in Children)
        {
            placement.Place(shape, Offset.Zero);
        }
        Dictionary<string, IReadOnlyList<double[]>> routes = Edges.ToDictionary(
            e => e.Id, e => e.Points(placement.Origins.GetValueOrDefault(e.Container ?? "", Offset.Zero)));
        List<Edge> edges = [.. source.Edges.Select(e => e with { Route = routes[e.Id] })];
        return new LayoutData(new Size(Width, Height), placement.Groups, placement.Nodes, edges);
    }
}
