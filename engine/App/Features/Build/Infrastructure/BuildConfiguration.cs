namespace Build.Infrastructure;

public static class BuildConfiguration {
    static readonly string[] Names = ["Debug", "Release"];

    public static string? Of(string bin) =>
        bin.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).AsEnumerable().Reverse()
            .Select(Name).FirstOrDefault(name => name is not null);

    static string? Name(string folder) =>
        Names.FirstOrDefault(name => string.Equals(folder.Split('_')[0], name, StringComparison.OrdinalIgnoreCase));
}
