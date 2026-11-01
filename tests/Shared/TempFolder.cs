namespace Tests.Shared;

// A short folder in the temporary folder. Windows has a 260-character path limit, so the names are short.
public static class TempFolder {
    public static string Create() {
        var path = Path.Combine(Path.GetTempPath(), "vsa", Guid.NewGuid().ToString("N")[..8]);
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
