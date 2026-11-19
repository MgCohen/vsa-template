namespace Changes.Infrastructure;

public static class BlockStamp {
    public const string Folder = "umpire-checks";
    public const string Extension = ".stamp";

    public static string In(string block, string view) =>
        Path.Combine(block, "artifacts~", Folder, view + Extension);

    public static string? Read(string file) => File.Exists(file) ? File.ReadAllText(file).Trim() : null;

    public static string Write(string file, string hash) {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, hash + "\n");
        return file;
    }
}
