namespace Umpire.Utility;

using System.Runtime.InteropServices;

public static class RelativePath {
    static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    static readonly OSPlatform[] CaseInsensitivePlatforms = [
        OSPlatform.Windows, OSPlatform.OSX, OSPlatform.Create("IOS"), OSPlatform.Create("TVOS"),
        OSPlatform.Create("WATCHOS"), OSPlatform.Create("MACCATALYST"),
    ];

    static StringComparison Comparison { get; } = CaseInsensitivePlatforms.Any(RuntimeInformation.IsOSPlatform)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static string Of(string relativeTo, string path) {
        var from = Path.GetFullPath(relativeTo);
        var to = Path.GetFullPath(path);
        if (!string.Equals(Path.GetPathRoot(from), Path.GetPathRoot(to), Comparison)) {
            return to;
        }
        var fromSegments = from.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var toSegments = to.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var common = 0;
        while (common < fromSegments.Length && common < toSegments.Length
            && string.Equals(fromSegments[common], toSegments[common], Comparison)) {
            common++;
        }
        var relative = string.Join(
            Path.DirectorySeparatorChar.ToString(),
            Enumerable.Repeat("..", fromSegments.Length - common).Concat(toSegments.Skip(common)));
        if (relative.Length == 0) {
            return ".";
        }
        return common < toSegments.Length && EndsInSeparator(to) ? relative + Path.DirectorySeparatorChar : relative;
    }

    static bool EndsInSeparator(string path) => path.Length > 0 && Separators.Contains(path[path.Length - 1]);
}
