using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Definitions;

public static class DefinitionGraph
{
    public static LayoutData Of(Definition definition, IReadOnlyList<RuleChange> changes)
    {
        PartTree tree = new(definition.Parts);
        IReadOnlyList<RuleEdge> edges = RuleEdges.Of(definition, changes);
        List<Part> parts = [.. definition.Parts.Where(p => p.Parent is not null)];
        List<Group> groups = [.. parts.Where(p => tree.HasChildren(p.Id)).Select(p => GroupOf(tree, p))];
        List<Node> nodes = [.. parts.Select(p => NodeOf(tree, p, edges.Where(e => e.IsSelf && e.FromPart == p.Id)))];
        return new LayoutData(new Size(0, 0), groups, nodes, [.. edges.Where(e => !e.IsSelf).Select(EdgeOf)]);
    }

    public static LayoutData Refresh(LayoutData placed, Definition definition, IReadOnlyList<RuleChange> changes)
    {
        LayoutData fresh = Of(definition, changes);
        Dictionary<string, Node> nodes = placed.Nodes.ToDictionary(n => n.Id);
        Dictionary<string, Group> groups = placed.Groups.ToDictionary(g => g.Id);
        return fresh with
        {
            Size = placed.Size,
            Nodes = [.. fresh.Nodes.Select(n => nodes.TryGetValue(n.Id, out Node? at) ? Placed(n, at) : n)],
            Groups = [.. fresh.Groups.Select(g => groups.TryGetValue(g.Id, out Group? at) ? Placed(g, at) : g)],
        };
    }

    private static Node Placed(Node node, Node at)
    {
        return node with { X = at.X, Y = at.Y };
    }

    private static Group Placed(Group group, Group at)
    {
        return group with { X = at.X, Y = at.Y, W = at.W, H = at.H };
    }

    private static string GroupId(string partId)
    {
        return $"g:{partId}";
    }

    private static Group GroupOf(PartTree tree, Part part)
    {
        string? parent = tree[part.Parent!].Parent is null ? null : GroupId(part.Parent!);
        string kind = part.Many ? "many" : "part";
        return new Group(GroupId(part.Id), parent, part.Layer, kind, part.Path, tree.Depth(part.Id) - 1, 0, 0, 0, 0);
    }

    private static Node NodeOf(PartTree tree, Part part, IEnumerable<RuleEdge> self)
    {
        bool whole = tree.HasChildren(part.Id);
        string badge = part.Many ? "many" : whole ? "whole part" : tree[part.Parent!].Layer;
        List<Row> rows = [.. part.OfType
            .Select((type, k) => new Row($"{part.Id}:t{k}", $"OfType {type}", "type", false, [type]))
            .Concat(self.Select(SelfRow))];
        string group = GroupId(whole ? part.Id : part.Parent!);
        return new Node(part.Id, group, part.Layer, badge, part.Path, 0, 0, 0, 0, rows);
    }

    private static Row SelfRow(RuleEdge edge)
    {
        string label = $"{AccessRules.Verb(edge.Access)} itself{(edge.Label is null ? "" : $" {edge.Label}")}";
        return new(edge.Id, label, edge.Access == Access.Deny ? "deny" : "rule", edge.Pending, []);
    }

    private static Edge EdgeOf(RuleEdge edge)
    {
        string rule = edge.Removed ? $"{edge.Text} (removed)" : edge.Pending ? $"{edge.Text} (pending)" : edge.Text;
        Use use = new(TermText.Of(edge.From), TermText.Of(edge.To), edge.Shown, rule, false, edge.Id, edge.Removed);
        return new(
            edge.Id, edge.FromPart, null, edge.ToPart, null, edge.Shown, [use], [], edge.Label, edge.Pending,
            edge.Removed);
    }
}
