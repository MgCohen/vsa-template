using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Shapes;

public sealed class PartNode : INodeShape
{
    public const double Width = 250;
    public const double Header = 44;
    public const double Row = 21;
    public const double Gap = 3;

    public string Name => "Part";

    public Node Shape(Node node)
    {
        double height = Header + (node.Rows.Count * Row) + (node.Rows.Count > 0 ? 2 * Gap : 0);
        List<Row> rows = [.. node.Rows.Select((row, k) => row with { Y = Header + Gap + (k * Row), H = Row })];
        return node with { W = Width, H = height, Ports = NodePorts.Of(Header / 2), Rows = rows, Header = Header };
    }
}
