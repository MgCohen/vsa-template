namespace Tests.Shared;

public static class LocalFeed {
    public const string Version = "0.0.0-test";

    public static readonly IReadOnlyList<string> PackRoots = ["architecture", "framework", "packs", "engine", "tools"];

    static readonly Lazy<string> Feed = new(Pack, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string Path => Feed.Value;

    public static IReadOnlyList<string> Packages =>
        [.. Directory.EnumerateFiles(Path, "*.nupkg").Select(f => System.IO.Path.GetFileName(f)[..^$".{Version}.nupkg".Length])];

    static string Pack() {
        var root = TempFolder.Create();
        var feed = System.IO.Path.Combine(root, "feed");
        var projects = PackRoots
            .SelectMany(root => Directory.EnumerateFiles(Repo.Path(root), "*.csproj", SearchOption.AllDirectories))
            .Select(p => $"  <Project Path=\"{System.IO.Path.GetRelativePath(root, p).Replace('\\', '/')}\" />");
        var solution = System.IO.Path.Combine(root, "pack.slnx");
        File.WriteAllLines(solution, ["<Solution>", .. projects, "</Solution>"]);
        Dotnet.Run(root, null, "pack", solution, "-c", "Release", "-o", feed, $"-p:Version={Version}",
            "-p:UseArtifactsOutput=true", $"-p:ArtifactsPath={System.IO.Path.Combine(root, "a")}", "-nodeReuse:false").EnsureSuccess();
        return feed;
    }
}
