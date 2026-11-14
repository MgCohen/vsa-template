namespace ArchitectureViewer.Domain.Shared;

public sealed record Group(
    string Id,
    string? Parent,
    string Title,
    string Kind,
    string Path,
    int Depth,
    double X,
    double Y,
    double W,
    double H);
