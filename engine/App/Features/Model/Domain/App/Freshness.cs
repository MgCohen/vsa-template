namespace Umpire.CheckModel;

static class Freshness {
    public static IEnumerable<string> OldFiles(LoadedSnapshot snapshot, IFileState files) => snapshot.Snapshot.Files
        .Where(f => f.Hash.Length > 0)
        .Where(f => !files.IsCurrent(snapshot.FileOf(f), f.Hash, f.Modified))
        .Select(snapshot.FileOf);

    public static bool IsCurrent(FileMark? mark, string block, IFileState files) => mark is not null && files.IsCurrent(mark, block);
}
