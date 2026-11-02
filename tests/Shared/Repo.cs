using System.Reflection;

namespace Tests.Shared;

public static class Repo {
    // tests/Directory.Build.props records the repository root on each test assembly.
    public static string Root { get; } =
        typeof(Repo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "RepoRoot").Value!;

    public static string Path(params string[] parts) => System.IO.Path.Combine([Root, .. parts]);
}
