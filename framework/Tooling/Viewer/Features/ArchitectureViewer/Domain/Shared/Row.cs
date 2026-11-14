namespace ArchitectureViewer.Domain.Shared;

public sealed record Row(
    string Id,
    string Label,
    string Role,
    bool More,
    IReadOnlyList<string> Types,
    double Y = 0,
    double H = 0);
