namespace Umpire.CheckModel;

public sealed record LoadedSnapshot(string Root, Snapshot Snapshot) {
    public static LoadedSnapshot Of(string block, Snapshot snapshot) => new(snapshot.Root is { Length: > 0 } root ? root : block, snapshot);

    public string FileOf(FileFacts file) => BlockPath.In(Root, file.Path);
}
