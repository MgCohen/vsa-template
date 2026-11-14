namespace ArchitectureViewer.Domain.Shared;

public sealed record Focus(string? NodeId, string? RowId, string? PortId = null, string? EdgeId = null)
{
    public static readonly Focus None = new(null, null);
}
