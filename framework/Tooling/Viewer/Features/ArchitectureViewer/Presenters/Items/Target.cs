namespace ArchitectureViewer.Presenters.Items;

public sealed record Target(string? NodeId, string? RowId, string? PortId = null, string? EdgeId = null)
{
    public static readonly Target None = new(null, null);

    public bool IsNone => NodeId is null && EdgeId is null;

    public static Target OnEdge(string edgeId)
    {
        return new(null, null, null, edgeId);
    }
}
