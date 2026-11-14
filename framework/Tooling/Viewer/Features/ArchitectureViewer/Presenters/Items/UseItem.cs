namespace ArchitectureViewer.Presenters.Items;

public sealed record UseItem(
    string From,
    string To,
    string Access,
    string Rule,
    bool Demo,
    string? EdgeId,
    bool Removed);
