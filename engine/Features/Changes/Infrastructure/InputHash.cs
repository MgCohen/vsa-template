namespace Changes.Infrastructure;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Umpire.Architecture;

public static class InputHash {
    const string ScriptAssemblies = "Library/ScriptAssemblies";
    static readonly string[] BinPatterns = ["*.dll", "*.pdb", "*.exe", "*.deps.json"];
    static readonly string[] InputNames = [".umpireconfig", "global.json"];
    static readonly string[] LargeFolders = ["Library", "Temp", "Logs", "UserSettings", "node_modules"];
    static readonly EnumerationOptions OneLevel = new() { IgnoreInaccessible = true };

    public static string Of(string block, string bin, string run) {
        var lines = new List<string> { run, Engine() };
        lines.AddRange(Facts(Patterns(bin, BinPatterns)));
        lines.AddRange(Facts(Patterns(Path.Combine(block, ScriptAssemblies), ["*.dll"])));
        lines.AddRange(ConfigFacts(block));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        return Convert.ToHexString(bytes);
    }

    public static bool IsInput(string file) {
        var name = Path.GetFileName(file);
        return InputNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            || name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || (name.StartsWith("Directory.", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".props", StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> ConfigFacts(string block) => [.. Facts(BlockFiles(block)), .. Facts(FilesAbove(block))];

    public static string Engine() => typeof(InputHash).Assembly.ManifestModule.ModuleVersionId.ToString();

    static IEnumerable<string> Patterns(string folder, string[] patterns) =>
        Directory.Exists(folder) ? patterns.SelectMany(pattern => Directory.EnumerateFiles(folder, pattern, OneLevel)) : [];

    static IEnumerable<string> BlockFiles(string folder) {
        if (!Directory.Exists(folder)) {
            return [];
        }
        var files = Directory.EnumerateFiles(folder, "*", OneLevel).Where(IsInput);
        var folders = Directory.EnumerateDirectories(folder, "*", OneLevel).Where(sub => !IsSkipped(Path.GetFileName(sub)));
        return files.Concat(folders.SelectMany(BlockFiles));
    }

    static IEnumerable<string> FilesAbove(string block) {
        var start = Directory.GetParent(Path.GetFullPath(block));
        for (var folder = start; folder is not null; folder = folder.Parent) {
            foreach (var file in Directory.EnumerateFiles(folder.FullName, "*", OneLevel).Where(IsInput)) {
                yield return file;
            }
        }
    }

    static bool IsSkipped(string name) =>
        BuildOutput.IsIgnored(name) || LargeFolders.Contains(name, StringComparer.OrdinalIgnoreCase);

    static IEnumerable<string> Facts(IEnumerable<string> files) => files.Order(StringComparer.Ordinal).Select(Fact);

    static string Fact(string file) {
        var info = new FileInfo(file);
        return string.Create(CultureInfo.InvariantCulture, $"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}");
    }
}
