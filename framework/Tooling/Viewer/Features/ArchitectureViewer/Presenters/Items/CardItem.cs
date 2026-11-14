namespace ArchitectureViewer.Presenters.Items;

public sealed record CardItem(
    string Id,
    string Layer,
    string Group,
    string Path,
    double X,
    double Y,
    double W,
    double H,
    IReadOnlyList<RowItem> Rows,
    IReadOnlyList<PortItem> Ports,
    double Header);
