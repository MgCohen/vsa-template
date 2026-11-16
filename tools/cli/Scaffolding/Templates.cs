namespace Umpire.Cli.Scaffolding;

using System.Text.RegularExpressions;

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
    const string DevProps = "  <Import Project=\"$([MSBuild]::GetPathOfFileAbove('Umpire.Sdk.Dev.props'))\" />\n";
    const string DevTargets = "  <Import Project=\"$(UmpireSdkDir)Sdk.targets\" />\n";
    const string AddedSdk = "  <Sdk Name=\"Umpire.Sdk\" />\n";
    const string BuildHook = "Umpire.Engine.Build";

    public static IReadOnlyList<TemplateFile> Files(
        Kind kind,
        IReadOnlyDictionary<string, string> names,
        string version) {
        var assembly = typeof(Templates).Assembly;
        var files = new List<TemplateFile>();
        foreach (var resource in Resources().Where(resource => KindOf(PathOf(resource)) == kind)) {
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
            files.Add(new TemplateFile(Replace(target, names), Replace(content, names)));
        }
        return files;
    }

    public static Kind KindOf(string pathInTemplates) {
        var inModuleFolder = pathInTemplates.StartsWith($"Features/{Module}/", StringComparison.Ordinal)
            || Path.GetFileName(pathInTemplates).Contains(Module, StringComparison.Ordinal);
        return pathInTemplates.Contains(Request, StringComparison.Ordinal) ? Kind.Request
            : pathInTemplates.Contains(Command, StringComparison.Ordinal) ? Kind.Command
            : pathInTemplates.Contains(Plugin, StringComparison.Ordinal) ? Kind.Plugin
            : pathInTemplates.Contains(Pack, StringComparison.Ordinal) ? Kind.Pack
            : inModuleFolder ? Kind.Module
            : Kind.App;
    }

    public static IEnumerable<string> All() => Resources().Select(PathOf);

    internal static string ToPackageMode(string content, string version, string path) {
        var hasDevProps = content.Contains(DevProps, StringComparison.Ordinal);
        if (hasDevProps && !content.StartsWith("<Project>", StringComparison.Ordinal)) {
            content = content.Replace(DevProps, AddedSdk).Replace(DevTargets, "");
        } else if (hasDevProps) {
            content = content.Replace(DevProps, "")
                .Replace(DevTargets, "")
                .Replace("<Project>", "<Project Sdk=\"Umpire.Sdk\">");
            content = Empty().Replace(content, "<Project Sdk=\"Umpire.Sdk\" />");
        }
        content = SourceReference().Replace(
            content,
            m => $"<PackageReference Include=\"{m.Groups[1].Value}\" Version=\"{version}\" />");
        if (IsRootProject(path)) {
            content = content.Replace(
                "</Project>",
                $"  <ItemGroup>\n    <PackageReference Include=\"{BuildHook}\" "
                + $"Version=\"{version}\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>",
                StringComparison.Ordinal);
        }
        if (content.Contains("$(Umpire", StringComparison.Ordinal)) {
            throw new UmpireException(
                $"The template {path} has a source-mode property that the tool cannot change. "
                + "Report this error to the Umpire maintainers.");
        }
        return content;
    }

    static bool IsRootProject(string path) => !path.Contains('/', StringComparison.Ordinal);

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

    [GeneratedRegex(@"^<Project Sdk=""Umpire.Sdk"">\s*</Project>")]
    private static partial Regex Empty();

    [GeneratedRegex(@"<ProjectReference Include=""\$\(UmpireFrameworkSourceDir\)[^""]*/(Umpire\.[\w.]+)\.csproj"" />")]
    private static partial Regex SourceReference();

    public enum Kind { App, Module, Request, Command, Plugin, Pack }
}
