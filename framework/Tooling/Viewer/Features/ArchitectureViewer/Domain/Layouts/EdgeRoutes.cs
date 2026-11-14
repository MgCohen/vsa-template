using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Layouts;

public static class EdgeRoutes
{
    public static IReadOnlyList<double[]> Moved(Edge edge, Node from, Node to, Offset fromAt, Offset toAt)
    {
        double[] start = Ports.Point(from, Ports.Out(from, edge.FromRow), fromAt);
        double[] end = Ports.Point(to, Ports.In(to, edge.ToRow), toAt);
        double midX = (start[0] + end[0]) / 2;
        return [start, [midX, start[1]], [midX, end[1]], end];
    }
}
