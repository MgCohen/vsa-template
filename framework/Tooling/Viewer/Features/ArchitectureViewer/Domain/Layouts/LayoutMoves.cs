using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain.Layouts;

public static class LayoutMoves
{
    public static LayoutData Move(LayoutData layout, string nodeId, Offset by)
    {
        return layout with
        {
            Nodes = [.. layout.Nodes.Select(n => n.Id == nodeId ? n with { X = n.X + by.X, Y = n.Y + by.Y } : n)],
        };
    }
}
