namespace Changes.Infrastructure;

public static class StampFile {
    public const string Name = ".umpire-check";

    public static string In(string bin) => Path.Combine(bin, Name);

    public static string? Read(string bin) => File.Exists(In(bin)) ? File.ReadAllText(In(bin)).Trim() : null;

    public static string Write(string bin, string hash) {
        Directory.CreateDirectory(bin);
        File.WriteAllText(In(bin), hash + "\n");
        return In(bin);
    }

    public static void Delete(string bin) => File.Delete(In(bin));
}
