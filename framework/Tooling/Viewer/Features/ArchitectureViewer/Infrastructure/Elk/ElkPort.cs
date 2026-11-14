using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkPort(
    string Id,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyDictionary<string, string> LayoutOptions)
{
    public static string IdOf(Node node, Port port)
    {
        return $"{node.Id}#{port.Id}";
    }

    public static List<ElkPort> Of(Node node)
    {
        return [.. (node.Ports ?? []).Select(p => p.Side == PortSide.Out
            ? new ElkPort(IdOf(node, p), node.W, p.Y, 1, 1, ElkOptions.East)
            : new ElkPort(IdOf(node, p), -1, p.Y, 1, 1, ElkOptions.West))];
    }
}
