namespace Umpire.CheckModel;

static class Freshness {
    public static bool IsCurrent(string path, string hash, long modified) {
        try {
            var file = new FileInfo(path);
            return file.Exists && (file.LastWriteTimeUtc.Ticks == modified || FileHash.Of(path) == hash);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    public static IEnumerable<string> OldFiles(LoadedSnapshot snapshot) => snapshot.Snapshot.Files
        .Where(f => f.Hash.Length > 0)
        .Where(f => !IsCurrent(snapshot.FileOf(f), f.Hash, f.Modified))
        .Select(snapshot.FileOf);
}
