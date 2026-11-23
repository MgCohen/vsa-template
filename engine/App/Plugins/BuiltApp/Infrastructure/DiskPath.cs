namespace BuiltApp.Infrastructure;

public static class DiskPath {
    public static string Of(string path) {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var root = Path.GetPathRoot(full)!;
        var current = OperatingSystem.IsWindows() ? root.ToUpperInvariant() : root;
        foreach (var name in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)) {
            current = Path.Combine(current, OnDisk(current, name));
        }
        return current;
    }

    static string OnDisk(string folder, string name) {
        try {
            return Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder, name).FirstOrDefault() is { } found
                ? Path.GetFileName(found)
                : name;
        } catch (Exception e) when (e is UnauthorizedAccessException or IOException) {
            return name;
        }
    }
}
