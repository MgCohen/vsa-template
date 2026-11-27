namespace Facts.Infrastructure;

public sealed record BlockFolderQuery(string Block, IReadOnlyList<string> Patterns);
