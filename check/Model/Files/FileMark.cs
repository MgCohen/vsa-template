namespace Umpire.CheckModel;

public sealed record FileMark(string Path, string Hash, long Modified) {
    public const string Unknown = "unknown";

    public bool IsAbsent => Hash.Length == 0;

    public static FileMark Of(string block, string file) {
        var path = BlockPath.Of(block, file);
        if (!File.Exists(file)) {
            return new FileMark(path, "", 0);
        }
        var modified = File.GetLastWriteTimeUtc(file).Ticks;
        return new FileMark(path, FileHash.Of(file), modified);
    }

    public bool IsCurrent(string block) {
        var file = BlockPath.In(block, Path);
        return IsAbsent ? !File.Exists(file) : Freshness.IsCurrent(file, Hash, Modified);
    }
}
