namespace Facts.Domain.Shared.Compile;

public sealed record ModelSettings(string? ProjectFolder, string? State, string? Mode, IReadOnlyList<string> SourceFolders, bool Saves) {
    public string? Snapshot { get; init; }

    public string? Target { get; init; }

    public string? Library { get; init; }

    public string? LibraryRoot { get; init; }
}
