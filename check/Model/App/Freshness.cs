namespace Umpire.CheckModel;

using System.Collections.Concurrent;

public sealed class Freshness {
    readonly ConcurrentDictionary<string, bool> current = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, FileFacts> files = new(StringComparer.OrdinalIgnoreCase);

    internal Freshness(IEnumerable<LoadedSnapshot> snapshots) {
        foreach (var snapshot in snapshots) {
            foreach (var file in snapshot.Snapshot.Files) {
                files[snapshot.FileOf(file)] = file;
            }
        }
    }

    public static bool IsCurrent(string path, FileFacts facts) {
        if (facts.Hash.Length == 0) {
            return true;
        }
        try {
            var file = new FileInfo(path);
            return file.Exists && (file.LastWriteTimeUtc.Ticks == facts.Modified || FileHash.Of(path) == facts.Hash);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    public bool IsCurrent(AppType type) => type.Files.All(f => !files.TryGetValue(f.File, out var facts)
        || current.GetOrAdd(f.File, file => IsCurrent(file, facts)));
}
