namespace Umpire.Architecture;

public static class BuildOutput {
    static readonly string[] Folders = ["obj", "bin"];
    static readonly char[] Separators = ['/', '\\'];

    public static bool IsFolder(string name) => Folders.Contains(name, StringComparer.OrdinalIgnoreCase);

    public static bool IsIgnored(string folder) => IsFolder(folder) || folder.StartsWith('.') || folder.EndsWith('~');

    public static bool Contains(string path) => path.Split(Separators)[..^1].Any(IsFolder);
}
