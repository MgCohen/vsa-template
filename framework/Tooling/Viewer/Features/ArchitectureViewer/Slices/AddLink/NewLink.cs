using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Layouts;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Slices.AddLink;

internal static class NewLink
{
    public static Edge? Of(LayoutData data, INodeShape shape, Focus from, Focus to)
    {
        Node? source = data.Nodes.FirstOrDefault(n => n.Id == from.NodeId);
        Node? target = data.Nodes.FirstOrDefault(n => n.Id == to.NodeId);
        if (source is null || target is null || source == target)
        {
            return null;
        }
        Port? output = Ports.Named(shape.Shape(source), from.PortId);
        Port? input = Ports.Named(shape.Shape(target), to.PortId);
        if (output?.Side != PortSide.Out || input?.Side != PortSide.In)
        {
            return null;
        }
        string fromLabel = Links.Label(source, output.RowId);
        string toLabel = Links.Label(target, input.RowId);
        Use use = new(fromLabel, toLabel, Access.Unchecked, "New link, not checked by the engine", false);
        string id = $"{source.Id}/{output.RowId}>{target.Id}/{input.RowId}";
        return new Edge(id, source.Id, output.RowId, target.Id, input.RowId, Access.Unchecked, [use], []);
    }

    public static bool Same(Edge a, Edge b)
    {
        return a.FromNode == b.FromNode && a.FromRow == b.FromRow && a.ToNode == b.ToNode && a.ToRow == b.ToRow;
    }
}
