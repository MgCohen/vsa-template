namespace ArchitectureViewer.Presenters.Items;

public sealed record PortItem(string Id, string? RowId, string Side, double Y)
{
    public bool Out => Side == "Out";
}
