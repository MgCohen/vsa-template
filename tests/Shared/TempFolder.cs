namespace Tests.Shared;

public static class TempFolder {
    internal static string Base => Path.Combine(Path.GetTempPath(), "umpire");

    public static string Create() {
        var path = Path.Combine(Base, Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(path);
        return path;
    }

    public static string Stable(string name) {
        var id = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name)))[..8].ToLowerInvariant();
        var path = Path.Combine(Base, $"s{id}");
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

    internal static void DeleteUnused(string keep, TimeSpan life) {
        var before = DateTime.UtcNow - life;
        foreach (var folder in Directory.EnumerateDirectories(Base).Where(folder => folder != keep)) {
            if (!Path.GetFileName(folder).StartsWith(LocalFeed.FeedPrefix, StringComparison.Ordinal)) {
                if (Directory.GetLastWriteTimeUtc(folder) < before) {
                    Delete(folder);
                }
                continue;
            }
            if (LastUse(folder) >= before) {
                continue;
            }
            using var taken = FeedLock.TryTake(folder);
            if (taken is not null && LastUse(folder) < before) {
                Delete(folder);
                File.Delete(FeedLock.PathOf(folder));
            }
        }
    }

    static DateTime LastUse(string feed) {
        var ready = LocalFeed.Ready(feed);
        return File.Exists(ready) ? File.GetLastWriteTimeUtc(ready) : Directory.GetLastWriteTimeUtc(feed);
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
