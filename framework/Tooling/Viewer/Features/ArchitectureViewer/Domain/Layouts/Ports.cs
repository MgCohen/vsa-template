using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Layouts;

public static class Ports
{
    public static Port Out(Node node, string? rowId)
    {
        return Find(node, rowId, PortSide.Out);
    }

    public static Port In(Node node, string? rowId)
    {
        return Find(node, rowId, PortSide.In);
    }

    public static Port? Named(Node node, string? portId)
    {
        return node.Ports?.FirstOrDefault(p => p.Id == portId);
    }

    public static double[] Point(Node node, Port port, Offset at)
    {
        return [node.X + (port.Side == PortSide.Out ? node.W + 1 : -1) + at.X, node.Y + port.Y + 0.5 + at.Y];
    }

    private static Port Find(Node node, string? rowId, PortSide side)
    {
        List<Port> ports = [.. (node.Ports ?? []).Where(p => p.Side == side)];
        return ports.FirstOrDefault(p => p.RowId == rowId) ?? ports.First(p => p.RowId is null);
    }
}
