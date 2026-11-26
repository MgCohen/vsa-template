namespace Umpire.Architecture;

using System.Runtime.InteropServices;

static class RelativePath {
    static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    static StringComparison Comparison { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    internal static string Of(string relativeTo, string path) {
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
