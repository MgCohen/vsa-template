namespace Umpire.CheckModel;

public sealed record FileMark(string Path, string Hash, long Modified) {
    public const string Unknown = "unknown";
    const string Sources = "*.cs";

    public bool IsAbsent => Hash.Length == 0;

    public static FileMark Of(string block, string file) {
        var path = BlockPath.Of(block, file);
        if (Directory.Exists(file)) {
            return new FileMark(path, FolderHash(file), 0);
        }
        if (!File.Exists(file)) {
            return new FileMark(path, "", 0);
        }
        var modified = File.GetLastWriteTimeUtc(file).Ticks;
        return new FileMark(path, FileHash.Of(file), modified);
    }

    public bool IsCurrent(string block) {
        var file = BlockPath.In(block, Path);
        if (Directory.Exists(file)) {
            return FolderHash(file) == Hash;
        }
        return IsAbsent ? !File.Exists(file) : Freshness.IsCurrent(file, Hash, Modified);
    }

    static string FolderHash(string folder) => FileHash.OfText(string.Join("|", Files(folder).OrderBy(f => f, StringComparer.Ordinal)));

    static IEnumerable<string> Files(string folder) => Directory.GetFiles(folder, Sources).Select(System.IO.Path.GetFileName)
        .Concat(Directory.GetDirectories(folder).Where(d => !BlockPath.IsIgnored(System.IO.Path.GetFileName(d)))
            .SelectMany(d => Files(d).Select(f => System.IO.Path.GetFileName(d) + "/" + f)));
}
