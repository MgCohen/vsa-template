namespace Tests.Shared;

using System.Reflection;

public static class Repo {
    public static string Root { get; } =
        typeof(Repo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "RepoRoot").Value!;

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);
}
