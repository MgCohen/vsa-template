namespace Changes.Infrastructure;

using System.Reflection;
using System.Text;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using Umpire.Utility;

public static class InputHash {
    static readonly string[] BinPatterns = ["*.dll", "*.pdb", "*.exe", "*.deps.json"];
    static readonly string[] InputNames = [".umpireconfig", "global.json"];
    static readonly string[] LargeFolders = ["node_modules"];
    static readonly FolderSearch OneLevel = new();
    static readonly FolderSearch FilesOfOneLevel = new() { AttributesToSkip = FileAttributes.System };

    public static string Of(
        string block, string bin, string run,
        IReadOnlyList<string>? references = null, IReadOnlyList<string>? ignoredFolders = null) {
        var lines = new List<string> { run, Engine() };
        lines.AddRange(Facts(Patterns(bin, BinPatterns)));
        lines.AddRange(Facts(Patterns(BinReader.DefinitionsFolder(bin), BinPatterns)));
        lines.AddRange(Facts((references ?? []).SelectMany(folder => Patterns(folder, ["*.dll", "*.pdb"]))));
        lines.AddRange(ConfigFacts(block, ignoredFolders));
        return PlatformPolyfills.Sha256Hex(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
    }

    public static bool IsInput(string file) {
        var name = Path.GetFileName(file);
        return InputNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            || name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || (name.StartsWith("Directory.", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".props", StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> ConfigFacts(string block, IReadOnlyList<string>? ignoredFolders = null) =>
        [
            .. Facts(BlockFiles(block, block, ignoredFolders ?? [])), .. Facts(FilesAbove(block)),
            .. DefinitionFolder.WithSource(block, ignoredFolders ?? [])];

    public static string Engine() => string.Join(",", new[] { typeof(InputHash).Assembly, Assembly.GetEntryAssembly() }
        .OfType<Assembly>()
        .Distinct()
        .Select(assembly => assembly.ManifestModule.ModuleVersionId.ToString()));

    static IEnumerable<string> Patterns(string folder, string[] patterns) =>
        Directory.Exists(folder) ? patterns.SelectMany(pattern => OneLevel.Files(folder, pattern)) : [];

    static IEnumerable<string> BlockFiles(string block, string folder, IReadOnlyList<string> ignoredFolders) {
        if (!Directory.Exists(folder)) {
            return [];
        }
        var files = FilesOfOneLevel.Files(folder, "*").Where(IsInput);
        var folders = OneLevel.Folders(folder, "*")
            .Where(sub => !IsSkipped(Path.GetFileName(sub)) && !IsIgnored(block, sub, ignoredFolders));
        return files.Concat(folders.SelectMany(sub => BlockFiles(block, sub, ignoredFolders)));
    }

    static IEnumerable<string> FilesAbove(string block) {
        var start = Directory.GetParent(Path.GetFullPath(block));
        for (var folder = start; folder is not null; folder = folder.Parent) {
            foreach (var file in FilesOfOneLevel.Files(folder.FullName, "*").Where(IsInput)) {
                yield return file;
            }
        }
    }

    static bool IsIgnored(string block, string folder, IReadOnlyList<string> ignoredFolders) =>
        ignoredFolders.Count > 0
        && ReadOptions.IsIgnored(RelativePath.Of(block, folder) + "/", ignoredFolders);

    static bool IsSkipped(string name) =>
        BuildOutput.IsIgnored(name) || LargeFolders.Contains(name, StringComparer.OrdinalIgnoreCase);

    static IEnumerable<string> Facts(IEnumerable<string> files) => files.Order(StringComparer.Ordinal).Select(Fact);

    static string Fact(string file) {
        var info = new FileInfo(file);
        return FormattableString.Invariant($"{info.FullName}|{info.Length}|{info.LastWriteTimeUtc.Ticks}");
    }
}
