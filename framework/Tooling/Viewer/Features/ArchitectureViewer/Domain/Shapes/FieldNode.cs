using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Shapes;

public sealed class FieldNode : INodeShape
{
    public const double Width = 268;
    public const double Header = 34;
    public const double Row = 28;

    public string Name => "Fields";

    public Node Shape(Node node)
    {
        List<Port> ports = [.. node.Rows
            .SelectMany((row, k) => Pair(row.Id, $"{row.Id}:", Header + (k * Row) + (Row / 2)))];
        ports.AddRange(Pair(null, "", Header / 2));
        List<Row> rows = [.. node.Rows.Select((row, k) => row with { Y = Header + (k * Row), H = Row })];
        return node with { W = Width, H = Header + (rows.Count * Row), Ports = ports, Rows = rows, Header = Header };
    }

    private static Port[] Pair(string? rowId, string prefix, double y)
    {
        return [new($"{prefix}in", rowId, PortSide.In, y), new($"{prefix}out", rowId, PortSide.Out, y)];
    }
}
