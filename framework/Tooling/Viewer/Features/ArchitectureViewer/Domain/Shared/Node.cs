namespace ArchitectureViewer.Domain.Shared;

public sealed record Node(
    string Id,
    string Parent,
    string Layer,
    string Group,
    string Path,
    double X,
    double Y,
    double W,
    double H,
    IReadOnlyList<Row> Rows,
    IReadOnlyList<Port>? Ports = null,
    double Header = 0);
