using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Infrastructure.Elk;

public sealed class ElkPlacement(LayoutData source)
{
    private readonly Dictionary<string, Group> groups = source.Groups.ToDictionary(g => g.Id);
    private readonly Dictionary<string, Node> nodes = source.Nodes.ToDictionary(n => n.Id);

    public Dictionary<string, Offset> Origins { get; } = [];

    public List<Group> Groups { get; } = [];

    public List<Node> Nodes { get; } = [];

    public void Place(ElkShape shape, Offset parent)
    {
        Offset at = new(parent.X + shape.X, parent.Y + shape.Y);
        Origins[shape.Id] = at;
        if (groups.TryGetValue(shape.Id, out Group? group))
        {
            Groups.Add(group with { X = at.X, Y = at.Y, W = shape.Width, H = shape.Height });
        }
        else
        {
            Nodes.Add(nodes[shape.Id] with { X = at.X, Y = at.Y, W = shape.Width, H = shape.Height });
        }
        foreach (ElkShape child in shape.Children ?? [])
        {
            Place(child, at);
        }
    }
}
