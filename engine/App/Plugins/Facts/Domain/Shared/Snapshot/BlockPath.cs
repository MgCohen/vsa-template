namespace Umpire.CheckModel;

public static class BlockPath {
    static readonly char[] Separators = ['/', '\\'];
    static readonly string[] OutputFolders = ["bin", "obj"];

    public static string Full(string path) {
        var full = Path.GetFullPath(path);
        return full == Path.GetPathRoot(full) ? full : full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static string Of(string folder, string file) => IsUnder(folder, file)
        ? Full(file).Substring(Prefix(folder).Length).Replace('\\', '/')
        : Full(file).Replace('\\', '/');

    public static bool IsUnder(string folder, string file) =>
        Full(file).StartsWith(Prefix(folder), StringComparison.OrdinalIgnoreCase);

    public static string In(string folder, string path) => Path.IsPathRooted(path) ? Full(path) : Full(Path.Combine(folder, path));

    public static bool IsInOutput(string path) {
        var segments = path.Split(Separators);
        return segments.Take(segments.Length - 1).Any(IsOutput);
    }

    public static bool IsInIgnoredFolder(string path) {
        var segments = path.Split(Separators);
        return segments.Take(segments.Length - 1).Any(IsIgnored);
    }

    public static bool IsIgnored(string folderName) => IsOutput(folderName)
        || folderName.StartsWith(".", StringComparison.Ordinal) || folderName.EndsWith("~", StringComparison.Ordinal);

    static string Prefix(string folder) =>
        Full(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

    static bool IsOutput(string folderName) => OutputFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase);
}
