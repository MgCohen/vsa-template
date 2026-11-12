namespace Tests.Shared;

// A short folder in the temporary folder. Windows has a 260-character path limit, so the names are short.
public static class TempFolder {
    public static string Create() {
        var path = Path.Combine(Path.GetTempPath(), "vsa", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    // The same short folder for the same name in each run. A virus scanner can then use its cache for files that did not
    // change. The folder is empty at the start.
    public static string Stable(string name) {
        var id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name)))[..8].ToLowerInvariant();
        var path = Path.Combine(Path.GetTempPath(), "vsa", $"s{id}");
        if (Directory.Exists(path)) {
            Directory.Delete(path, recursive: true);
        }
        Directory.CreateDirectory(path);
        return path;
    }

    // A process can keep a file open for a short time after it stops. Then the folder stays until the next clean.
    public static void Delete(string path) {
        try {
            Directory.Delete(path, recursive: true);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
        }
    }

    // Copies a source folder without its bin and obj folders.
    public static void Copy(string from, string to) {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from)) {
            File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
        }
        foreach (var dir in Directory.EnumerateDirectories(from)) {
            var name = Path.GetFileName(dir);
            if (name is not ("bin" or "obj")) {
                Copy(dir, Path.Combine(to, name));
            }
        }
    }
}
