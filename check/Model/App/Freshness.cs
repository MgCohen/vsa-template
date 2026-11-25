namespace Umpire.CheckModel;

using System.Collections.Concurrent;

sealed class Freshness {
    readonly ConcurrentDictionary<string, bool> current = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, FileFacts> files = new(StringComparer.OrdinalIgnoreCase);

    public Freshness(IEnumerable<LoadedSnapshot> snapshots) {
        foreach (var snapshot in snapshots) {
            foreach (var file in snapshot.Snapshot.Files.Where(f => f.Hash.Length > 0)) {
                files[snapshot.FileOf(file)] = file;
            }
        }
    }

    public bool AreAllCurrent => files.Keys.All(IsCurrent);

    public static bool IsCurrent(string path, string hash, long modified) {
        try {
            var file = new FileInfo(path);
            return file.Exists && (file.LastWriteTimeUtc.Ticks == modified || FileHash.Of(path) == hash);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    public bool IsCurrent(AppType type) => type.Files.All(f => IsCurrent(f.File));

    public bool IsCurrent(string path) => !files.TryGetValue(path, out var facts)
        || current.GetOrAdd(path, file => IsCurrent(file, facts.Hash, facts.Modified));
}
