namespace Tests.Shared;

public static class TempFolder {
    public static string Create() {
        var path = Path.Combine(Path.GetTempPath(), "umpire", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    public static string Stable(string name) {
        var id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name)))[..8].ToLowerInvariant();
        var path = Path.Combine(Path.GetTempPath(), "umpire", $"s{id}");
        if (Directory.Exists(path)) {
            Directory.Delete(path, recursive: true);
        }
        Directory.CreateDirectory(path);
        return path;
    }

    public static void Delete(string path) {
        try {
            Directory.Delete(path, recursive: true);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
        }
    }

    public static void Copy(string from, string to) {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from)) {
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        }
        foreach (var dir in Directory.EnumerateDirectories(from)) {
            var name = Path.GetFileName(dir);
            if (name is not ("bin" or "obj" or "artifacts~")) {
                Copy(dir, Path.Combine(to, name));
            }
        }
    }
}
