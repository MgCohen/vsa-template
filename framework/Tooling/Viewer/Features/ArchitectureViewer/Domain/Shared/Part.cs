namespace ArchitectureViewer.Domain.Shared;

public sealed record Part(
    string Id,
    string Layer,
    string? Parent,
    string Path,
    bool Many,
    IReadOnlyList<string> OfType);
