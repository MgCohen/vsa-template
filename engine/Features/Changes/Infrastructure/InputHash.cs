namespace Changes.Infrastructure;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public static class InputHash {
    static readonly string[] BinFiles = ["*.dll", "*.pdb", "*.exe"];
    static readonly string[] BlockFiles = [".umpireconfig", "*.csproj"];
    static readonly string[] OutputFolders = ["artifacts~", "bin", "obj", ".git"];

    public static string Of(string block, string bin, string run) {
        var lines = new List<string> { run, Engine() };
        lines.AddRange(Files(bin, BinFiles, SearchOption.TopDirectoryOnly));
        lines.AddRange(Files(block, BlockFiles, SearchOption.AllDirectories).Where(line => !InOutput(block, line)));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexString(bytes);
    }

    static string Engine() => typeof(InputHash).Assembly.ManifestModule.ModuleVersionId.ToString();

    static IEnumerable<string> Files(string folder, string[] patterns, SearchOption depth) {
        if (!Directory.Exists(folder)) {
            return [];
        }
        return patterns.SelectMany(pattern => Directory.EnumerateFiles(folder, pattern, depth))
            .Order(StringComparer.Ordinal)
            .Select(Fact);
    }

    static string Fact(string file) {
        var info = new FileInfo(file);
        return string.Create(CultureInfo.InvariantCulture, $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}");
    }

    static bool InOutput(string block, string line) {
        var relative = Path.GetRelativePath(block, line.Split('|')[0]);
        return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => OutputFolders.Contains(part, StringComparer.OrdinalIgnoreCase));
    }
}
