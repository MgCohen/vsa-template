namespace Umpire.Cli.Scaffolding;

using System.Text.RegularExpressions;
using Umpire.Files;

static partial class Templates {
    public const string App = "SampleApp";
    public const string Module = "SampleModule";
    public const string Request = "SampleAction";
    public const string Command = "SampleChange";
    public const string Plugin = "SamplePlugin";
    public const string Pack = "SamplePack";
    public const string VersionToken = "{{UmpireVersion}}";
    const string ResourcePrefix = "Templates/";
    const string InactiveSuffix = ".template";
    const string SourceVersion = "$(UmpireVersion)";
    const string SourceNUnitVersion = "$(UmpireNUnitVersion)";
    const string SourceNUnitAdapterVersion = "$(UmpireNUnitAdapterVersion)";
    static readonly string[] SetupTemplates = [
        ".gitignore.template",
        ".umpireconfig",
        "Directory.Build.props.template",
        "nuget.config.template",
    ];

    public static IReadOnlyList<TextFile> Files(
        TemplateGroup group,
        IReadOnlyDictionary<string, string> names,
        string version) {
        var assembly = typeof(Templates).Assembly;
        var files = new List<TextFile>();
        foreach (var resource in Resources().Where(resource => GroupOf(PathOf(resource)) == group)) {
            var path = PathOf(resource);
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
            var content = reader.ReadToEnd()
                .Replace("\r\n", "\n")
                .Replace(VersionToken, version, StringComparison.Ordinal);
            var target = path.EndsWith(InactiveSuffix, StringComparison.Ordinal)
                ? path[..^InactiveSuffix.Length]
                : path;
            if (target.EndsWith(".csproj", StringComparison.Ordinal)) {
                content = ToPackageMode(content, version, target);
            }
            files.Add(new TextFile(Replace(target, names), Replace(content, names)));
        }
        return files;
    }

    public static IReadOnlyList<TextFile> WithCentralVersions(IReadOnlyList<TextFile> files, bool central) =>
        central
            ? [.. files.Select(file => file.Path.EndsWith(".csproj", StringComparison.Ordinal)
                ? file with { Content = CentralVersions.Overrides(file.Content) }
                : file)]
            : files;

    public static string SetupFile(string path, string version) =>
        Files(TemplateGroup.Setup, new Dictionary<string, string>(), version).Single(file => file.Path == path).Content;

    public static TemplateGroup GroupOf(string pathInTemplates) {
        var inModuleFolder = pathInTemplates.StartsWith($"App/Features/{Module}/", StringComparison.Ordinal)
            || Path.GetFileName(pathInTemplates).Contains(Module, StringComparison.Ordinal);
        return pathInTemplates.Contains(Request, StringComparison.Ordinal) ? TemplateGroup.Request
            : pathInTemplates.Contains(Command, StringComparison.Ordinal) ? TemplateGroup.Command
            : pathInTemplates.Contains(Plugin, StringComparison.Ordinal) ? TemplateGroup.Plugin
            : pathInTemplates.Contains(Pack, StringComparison.Ordinal) ? TemplateGroup.Pack
            : inModuleFolder ? TemplateGroup.Module
            : SetupTemplates.Contains(pathInTemplates) ? TemplateGroup.Setup
            : TemplateGroup.App;
    }

    public static IEnumerable<string> All() => Resources().Select(PathOf);

    internal static string ToPackageMode(string content, string version, string path) {
        content = content
            .Replace(SourceVersion, version, StringComparison.Ordinal)
            .Replace(SourceNUnitVersion, ToolVersion.NUnit, StringComparison.Ordinal)
            .Replace(SourceNUnitAdapterVersion, ToolVersion.NUnitAdapter, StringComparison.Ordinal);
        content = SourceReference().Replace(
            content,
            m => $"<PackageReference Include=\"{m.Groups[1].Value}\" Version=\"{version}\" />");
        if (content.Contains("$(Umpire", StringComparison.Ordinal)) {
            throw new UmpireException(
                $"The template {path} has a source-mode property that the tool cannot change. "
                + "Report this error to the Umpire maintainers.");
        }
        return content;
    }

    static IEnumerable<string> Resources() =>
        typeof(Templates).Assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .OrderBy(PathOf, StringComparer.Ordinal);

    static string PathOf(string resource) => resource[ResourcePrefix.Length..].Replace('\\', '/');

    static string Replace(string text, IReadOnlyDictionary<string, string> sampleToNewName) {
        foreach (var (sample, name) in sampleToNewName) {
            text = text.Replace(sample, name, StringComparison.Ordinal)
                .Replace(Names.Kebab(sample), Names.Kebab(name), StringComparison.Ordinal);
        }
        return text;
    }

    [GeneratedRegex(@"<ProjectReference Include=""\$\(UmpireFrameworkSourceDir\)[^""]*/(Umpire\.[\w.]+)\.csproj"" />")]
    private static partial Regex SourceReference();

}
