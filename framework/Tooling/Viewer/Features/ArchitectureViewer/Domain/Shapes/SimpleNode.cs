using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Shapes;

public sealed class SimpleNode : INodeShape
{
    public const double Width = 220;
    public const double Height = 58;

    public string Name => "Simple";

    public Node Shape(Node node)
    {
        return node with { W = Width, H = Height, Ports = NodePorts.Of(Height / 2), Header = Height };
    }
}
