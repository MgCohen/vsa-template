namespace ArchitectureViewer.Infrastructure.Elk;

public sealed record ElkShape(
    string Id,
    double X,
    double Y,
    double Width,
    double Height,
    IReadOnlyList<ElkShape>? Children);
