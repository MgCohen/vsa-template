using ArchitectureViewer.Domain.Shared;
using ArchitectureViewer.Presenters.Items;

namespace ArchitectureViewer.Presenters;

internal static class Mapping
{
    public static readonly LayoutData Empty = new(new Size(0, 0), [], [], []);

    public static CardItem CardOf(Node node)
    {
        List<RowItem> rows = [.. node.Rows.Select(r => new RowItem(r.Id, r.Label, r.Role, r.More, r.Y, r.H))];
        List<PortItem> ports = [.. (node.Ports ?? []).Select(p => new PortItem(p.Id, p.RowId, p.Side.ToString(), p.Y))];
        return new(
            node.Id, node.Layer, node.Group, node.Path, node.X, node.Y, node.W, node.H, rows, ports, node.Header);
    }

    public static GroupItem GroupOf(Group group)
    {
        return new(group.Id, group.Title, group.Kind, group.Depth, group.X, group.Y, group.W, group.H);
    }

    public static LinkItem LinkOf(Edge edge)
    {
        List<UseItem> uses = [.. edge.Uses.Select(UseOf)];
        return new(
            edge.Id, edge.FromNode, edge.ToNode, edge.Access, edge.Label, edge.Pending, edge.Removed, edge.Route, uses);
    }

    public static Focus FocusOf(Target target)
    {
        return new(target.NodeId, target.RowId, target.PortId, target.EdgeId);
    }

    private static UseItem UseOf(Use use)
    {
        return new(use.From, use.To, use.Access, use.Rule, use.Demo, use.EdgeId, use.Removed);
    }
}
