namespace Facts.Infrastructure;

using Umpire.Architecture;

public sealed record TypeFactsQuery(
    IReadOnlyList<string> Dlls, IReadOnlyDictionary<string, Source> Sources, string BlockFolder,
    ResolvedArchitecture Architecture, IReadOnlyList<string> IgnoredFolders);
