using System.Text.RegularExpressions;

namespace VSA.Cli.Scaffolding;

// Template format A: the templates are one compilable app with fixed sample names. The tool replaces the names.
// The path of a template file gives its command, in this order:
//   1. the path contains SampleAction                                  add request
//   2. the path contains SampleChange, or the file is one of the two
//      command projects (Projects/SampleModule.*.Commands.csproj)      add command
//   3. the path is under Modules/SampleModule/, or the file name
//      contains SampleModule (App/Views/Pages/SampleModulePage.razor)  add module
//   4. all other files                                                 new app
// A ".template" suffix keeps a file inactive in this repository, for example AGENTS.md.template. The tool removes it.
static partial class Templates {
    public const string App = "SampleApp";
    public const string Module = "SampleModule";
    public const string Request = "SampleAction";
    public const string Command = "SampleChange";
    const string Prefix = "Templates/";
    const string Suffix = ".template";

    public enum Kind { App, Module, Request, Command }

    // The files of one command: (path relative to the app root, content), with the sample names replaced.
    public static IReadOnlyList<(string Path, string Content)> Files(Kind kind, IReadOnlyDictionary<string, string> names, string version) {
        var assembly = typeof(Templates).Assembly;
        var files = new List<(string, string)>();
        foreach (var resource in assembly.GetManifestResourceNames().Where(r => r.StartsWith(Prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal)) {
            var path = resource[Prefix.Length..].Replace('\\', '/');
            if (KindOf(path) != kind) {
                continue;
            }
            using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);
            var content = reader.ReadToEnd().Replace("\r\n", "\n");
            if (path.EndsWith(Suffix, StringComparison.Ordinal)) {
                path = path[..^Suffix.Length];
            }
            if (path.EndsWith(".csproj", StringComparison.Ordinal)) {
                content = ToPackageMode(content, version, path);
            }
            files.Add((Replace(path, names), Replace(content, names)));
        }
        return files;
    }

    // path: relative to Templates/, with "/".
    public static Kind KindOf(string path) =>
        path.Contains(Request, StringComparison.Ordinal) ? Kind.Request
        : path.Contains(Command, StringComparison.Ordinal) || CommandProjects.Contains(path) ? Kind.Command
        : path.StartsWith($"Modules/{Module}/", StringComparison.Ordinal) || Path.GetFileName(path).Contains(Module, StringComparison.Ordinal) ? Kind.Module
        : Kind.App;

    // A module gets the command projects with its first command, so a module with no command has no empty projects.
    public static readonly string[] CommandProjects = [
        $"Modules/{Module}/Projects/{Module}.Contracts.Commands.csproj",
        $"Modules/{Module}/Projects/{Module}.Handlers.Commands.csproj",
    ];

    // The path of each template file, relative to Templates/, before the names are replaced.
    public static IEnumerable<string> All() =>
        typeof(Templates).Assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(r => r[Prefix.Length..].Replace('\\', '/'))
            .Order(StringComparer.Ordinal);

    // names: sample name to new name. The kebab form of each name, for routes, is replaced too.
    static string Replace(string text, IReadOnlyDictionary<string, string> names) {
        foreach (var (sample, name) in names) {
            text = text.Replace(sample, name, StringComparison.Ordinal).Replace(Names.Kebab(sample), Names.Kebab(name), StringComparison.Ordinal);
        }
        return text;
    }

    // The templates build in this repository in source mode, as the samples do. A consumer uses packages:
    //   the VSA.Sdk.Dev.props imports           become Sdk="VSA.Sdk"
    //   a ProjectReference into framework/      becomes a PackageReference with the version of this tool
    internal static string ToPackageMode(string content, string version, string path) {
        if (content.Contains(DevProps, StringComparison.Ordinal)) {
            content = content.Replace(DevProps, "").Replace(DevTargets, "").Replace("<Project>", "<Project Sdk=\"VSA.Sdk\">");
            content = Empty().Replace(content, "<Project Sdk=\"VSA.Sdk\" />");
        }
        content = SourceReference().Replace(content, m => $"<PackageReference Include=\"{m.Groups[1].Value}\" Version=\"{version}\" />");
        if (content.Contains("$(Vsa", StringComparison.Ordinal)) {
            throw new VsaException($"The template {path} has a source-mode property that the tool cannot change. Report this error to the VSA maintainers.");
        }
        return content;
    }

    const string DevProps = "  <Import Project=\"$([MSBuild]::GetPathOfFileAbove('VSA.Sdk.Dev.props'))\" />\n";
    const string DevTargets = "  <Import Project=\"$(VsaSdkDir)Sdk.targets\" />\n";

    [GeneratedRegex(@"^<Project Sdk=""VSA.Sdk"">\s*</Project>")]
    private static partial Regex Empty();

    [GeneratedRegex(@"<ProjectReference Include=""\$\(VsaFrameworkSourceDir\)[^""]*/(VSA\.[\w.]+)\.csproj"" />")]
    private static partial Regex SourceReference();
}
