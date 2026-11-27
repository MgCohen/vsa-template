namespace Facts.Infrastructure;

using System.Runtime.InteropServices;

public static class SharedFrameworks {
    const string AspNetCore = "Microsoft.AspNetCore.App";

    public static IReadOnlyList<string> Folders() =>
        Web() is { } web ? [Runtime(), web] : [Runtime()];

    public static string? Web() {
        var shared = Path.GetDirectoryName(Path.GetDirectoryName(PlatformPolyfills.TrimEndingDirectorySeparator(Runtime())));
        var web = shared is null ? null : Path.Combine(shared, AspNetCore);
        return web is not null && Directory.Exists(web)
            ? Directory.GetDirectories(web).OrderByDescending(VersionOf).FirstOrDefault()
            : null;
    }

    static string Runtime() => RuntimeEnvironment.GetRuntimeDirectory();

    static Version VersionOf(string folder) => Version.TryParse(Path.GetFileName(folder), out var version) ? version : new Version();
}
