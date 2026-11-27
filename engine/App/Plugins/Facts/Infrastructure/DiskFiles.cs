namespace Facts.Infrastructure;

using System.Security.Cryptography;
using Umpire.CheckModel;

public static class DiskFiles {
    const string Sources = "*.cs";

    public static string Hash(string file) {
        using var sha = SHA256.Create();
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return FileHash.Of(sha.ComputeHash(stream));
    }

    public static FileMark Mark(string block, string file) {
        var path = BlockPath.Of(block, file);
        if (Directory.Exists(file)) {
            return new FileMark(path, FolderHash(file), 0);
        }
        if (!File.Exists(file)) {
            return new FileMark(path, "", 0);
        }
        var modified = File.GetLastWriteTimeUtc(file).Ticks;
        return new FileMark(path, Hash(file), modified);
    }

    public static bool IsCurrent(FileMark mark, string block) {
        var file = BlockPath.In(block, mark.Path);
        try {
            if (Directory.Exists(file)) {
                return FolderHash(file) == mark.Hash;
            }
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return false;
        }
        return mark.IsAbsent ? !File.Exists(file) : IsCurrent(file, mark.Hash, mark.Modified);
    }

    public static bool IsCurrent(string path, string hash, long modified) {
        try {
            var file = new FileInfo(path);
            return file.Exists && (file.LastWriteTimeUtc.Ticks == modified || Hash(path) == hash);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return false;
        }
    }

    static string FolderHash(string folder) => FileHash.OfText(string.Join("|", Files(folder).OrderBy(f => f, StringComparer.Ordinal)));

    static IEnumerable<string> Files(string folder) => Directory.GetFiles(folder, Sources).Select(Path.GetFileName)
        .Concat(Directory.GetDirectories(folder).Where(d => !BlockPath.IsIgnored(Path.GetFileName(d)))
            .SelectMany(d => Files(d).Select(f => Path.GetFileName(d) + "/" + f)));
}
