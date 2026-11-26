namespace Umpire.Architecture;

public static class BuildOutput {
    static readonly string[] Folders = ["obj", "bin"];
    static readonly char[] Separators = ['/', '\\'];

    public static bool IsFolder(string name) => Folders.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsIgnored(string folderName) =>
        IsFolder(folderName) || folderName.StartsWith(".", StringComparison.Ordinal) || folderName.EndsWith("~", StringComparison.Ordinal);

    public static bool Contains(string path) {
        var segments = path.Split(Separators);
        return segments.Take(segments.Length - 1).Any(IsFolder);
    }
}
