namespace ArchitectureViewer.Domain.Shared;

public sealed record Use(
    string From,
    string To,
    string Access,
    string Rule,
    bool Demo,
    string? EdgeId = null,
    bool Removed = false);
