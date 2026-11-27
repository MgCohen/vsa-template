namespace Tests.Shared;

public static class LocalFeed {
    public const string Version = "0.0.0-test";

    internal const string FeedPrefix = "f-";

    public static readonly IReadOnlyList<string> PackRoots = ["architecture", "framework", "packs", "engine", "tools"];

    static readonly TimeSpan UnusedFeedLife = TimeSpan.FromHours(2);

    static readonly Lazy<string> Folder = new(Prepare, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string Path => System.IO.Path.Combine(Folder.Value, "feed");

    public static string NuGetCache => System.IO.Path.Combine(Folder.Value, "nuget");

    public static IReadOnlyList<string> Packages =>
        [.. Directory.EnumerateFiles(Path, "*.nupkg")
            .Select(f => System.IO.Path.GetFileName(f)[..^$".{Version}.nupkg".Length])];

    static string[] PackArguments(string root) =>
        ["pack", System.IO.Path.Combine(root, "pack.slnx"), "-c", "Release", "-o", System.IO.Path.Combine(root, "feed"),
            $"-p:Version={Version}", "-p:UseArtifactsOutput=true",
            $"-p:ArtifactsPath={System.IO.Path.Combine(root, "a")}", "-nodeReuse:false"];

    static string Prepare() {
        var hash = Hash();
        var root = System.IO.Path.Combine(TempFolder.Base, $"{FeedPrefix}{hash}");
        using (FeedLock.Wait(root)) {
            if (!File.Exists(Ready(root))) {
                Pack(root);
                if (Hash() == hash) {
                    File.WriteAllText(Ready(root), Version);
                }
            }
            if (File.Exists(Ready(root))) {
                File.SetLastWriteTimeUtc(Ready(root), DateTime.UtcNow);
            }
        }
        TempFolder.DeleteUnused(root, UnusedFeedLife);
        return root;
    }

    internal static string Ready(string root) => System.IO.Path.Combine(root, "ready");

    static string Hash() => FeedInputs.Hash(PackRoots, string.Join(' ', PackArguments("")));

    static void Pack(string root) {
        TempFolder.Delete(root);
        Directory.CreateDirectory(root);
        var projects = PackRoots
            .SelectMany(pack => Directory.EnumerateFiles(Repo.Path(pack), "*.csproj", SearchOption.AllDirectories))
            .Select(p => $"  <Project Path=\"{System.IO.Path.GetRelativePath(root, p).Replace('\\', '/')}\" />");
        File.WriteAllLines(System.IO.Path.Combine(root, "pack.slnx"), ["<Solution>", .. projects, "</Solution>"]);
        Dotnet.Run(root, null, PackArguments(root)).EnsureSuccess();
        TempFolder.Delete(System.IO.Path.Combine(root, "a"));
    }
}
