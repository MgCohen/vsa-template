using ArchitectureViewer.Domain.Shared;

namespace ArchitectureViewer.Domain;

public static class Links
{
    public static LinkChange Change(ChangeKind kind, Edge edge)
    {
        return new(kind, edge.FromNode, edge.FromRow, edge.ToNode, edge.ToRow);
    }

    public static string Label(Node node, string? rowId)
    {
        return node.Rows.FirstOrDefault(r => r.Id == rowId)?.Label ?? $"{node.Layer} ({node.Group})";
    }
}
