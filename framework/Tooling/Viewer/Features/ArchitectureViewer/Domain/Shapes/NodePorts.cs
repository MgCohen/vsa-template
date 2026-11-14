using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Shapes;

public static class NodePorts
{
    public static IReadOnlyList<Port> Of(double y)
    {
        return [new("in", null, PortSide.In, y), new("out", null, PortSide.Out, y)];
    }
}
