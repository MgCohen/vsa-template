namespace Umpire.CheckModel.Export;

public sealed record BlockInput(string Block, IReadOnlyList<string> Projects) {
    public IReadOnlyList<string> Libraries { get; init; } = [];
    public IReadOnlyList<string> InnerBlocks { get; init; } = [];
    public IReadOnlyList<StateFolder> OtherStates { get; init; } = [];
    public IReadOnlyList<string> IgnoredFolders { get; init; } = [];
    public IReadOnlyList<FolderAlias> FolderAliases { get; init; } = [];
    public IReadOnlyList<FileMark> DefinitionSources { get; init; } = [];
    public IReadOnlyList<string> InputFiles { get; init; } = [];
    public IEnumerable<KeyValuePair<string, string>> Config { get; init; } = [];
    public IReadOnlyList<SnapshotPlace> Snapshots { get; init; } = [];
}
