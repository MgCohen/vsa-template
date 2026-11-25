namespace Umpire.CheckModel;

public sealed record LoadedSnapshot(string Root, Snapshot Snapshot, DateTime? SavedAt = null) {
    public string FileOf(FileFacts file) => BlockPath.In(Root, file.Path);
}
