namespace Umpire.CheckModel;

using System.Collections.Concurrent;

static class SnapshotCache {
    static readonly ConcurrentDictionary<string, Entry> Entries = new(StringComparer.OrdinalIgnoreCase);

    public static Snapshot Read(string file) {
        var info = new FileInfo(file);
        var modified = info.LastWriteTimeUtc.Ticks;
        var length = info.Length;
        if (Entries.TryGetValue(file, out var entry) && entry.Modified == modified && entry.Length == length) {
            return entry.Snapshot;
        }
        var snapshot = Snapshot.Read(File.ReadAllText(file));
        Entries[file] = new Entry(modified, length, snapshot);
        return snapshot;
    }

    sealed record Entry(long Modified, long Length, Snapshot Snapshot);
}
