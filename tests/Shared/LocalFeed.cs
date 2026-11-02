namespace Tests.Shared;

// All framework packages and VSA.Sdk, packed one time for each test process to a feed in a temporary folder.
public static class LocalFeed {
    public const string Version = "0.0.0-test";

    static readonly Lazy<string> Feed = new(Pack, LazyThreadSafetyMode.ExecutionAndPublication);

    // The folder of the .nupkg files. The first call packs, so it takes some time.
    public static string Path => Feed.Value;

    // The package ids in the feed, for example VSA.Domain and VSA.Sdk.
    public static IReadOnlyList<string> Packages =>
        [.. Directory.EnumerateFiles(Path, "*.nupkg").Select(f => System.IO.Path.GetFileName(f)[..^$".{Version}.nupkg".Length])];

    static string Pack() {
        var root = TempFolder.Create();
        var feed = System.IO.Path.Combine(root, "feed");
        // A solution of only the framework projects: the samples and the tests do not ship.
        var projects = Directory.EnumerateFiles(Repo.Path("framework"), "*.csproj", SearchOption.AllDirectories)
            .Select(p => $"  <Project Path=\"{System.IO.Path.GetRelativePath(root, p).Replace('\\', '/')}\" />");
        var solution = System.IO.Path.Combine(root, "pack.slnx");
        File.WriteAllLines(solution, ["<Solution>", .. projects, "</Solution>"]);
        // The build output goes to the temporary folder, so the bin and obj folders of the repository do not change.
        Dotnet.Run(root, null, "pack", solution, "-c", "Release", "-o", feed, $"-p:Version={Version}",
            "-p:UseArtifactsOutput=true", $"-p:ArtifactsPath={System.IO.Path.Combine(root, "a")}", "-nodeReuse:false").EnsureSuccess();
        return feed;
    }
}
