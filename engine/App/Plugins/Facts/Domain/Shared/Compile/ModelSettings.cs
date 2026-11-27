namespace Facts.Domain.Shared.Compile;

public sealed record ModelSettings(string? ProjectFolder, string? State, string? Mode, IReadOnlyList<string> SourceFolders, bool Saves);
