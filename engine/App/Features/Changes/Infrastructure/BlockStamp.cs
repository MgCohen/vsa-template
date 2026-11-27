namespace Changes.Infrastructure;

public static class BlockStamp {
    public const string Folder = "umpire-checks";
    public const string Extension = ".stamp";

    public static string In(string block, string view) =>
        Path.Combine(block, "artifacts~", Folder, view + Extension);

    public static string? Read(string file) {
        try {
            return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return null;
        }
    }

    public static string Write(string file, string hash) {
        var temporary = $"{file}.{Guid.NewGuid():N}.tmp";
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(temporary, hash + Environment.NewLine);
            PlatformPolyfills.MoveOver(temporary, file);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            Remove(temporary);
        }
        return file;
    }

    static void Remove(string file) {
        try {
            File.Delete(file);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return;
        }
    }
}
