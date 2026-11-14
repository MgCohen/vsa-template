using System.Text.RegularExpressions;

namespace VSA.Cli.Scaffolding;

// Template format A: the templates are one compilable app with fixed sample names. The tool replaces the names.
// The path of a template file gives its command, in this order:
//   1. the path contains SampleAction                                  add request
//   2. the path contains SampleChange                                  add command
//   3. the path contains SamplePlugin (Plugins/SamplePlugin/)          add plugin
//   4. the path is under Features/SampleModule/, or the file name
//      contains SampleModule (App/Views/Pages/SampleModulePage.razor)  add module
//   5. all other files                                                 new app
// An app has one root project (SampleApp.csproj). A module, a request, a command and a plugin add only source files.
// A ".template" suffix keeps a file inactive in this repository, for example AGENTS.md.template. The tool removes it.
static partial class Templates {
    public const string App = "SampleApp";
    public const string Module = "SampleModule";
    public const string Request = "SampleAction";
    public const string Command = "SampleChange";
    public const string Plugin = "SamplePlugin";
    const string Prefix = "Templates/";
    const string Suffix = ".template";

    public enum Kind { App, Module, Request, Command, Plugin }

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
        : path.Contains(Command, StringComparison.Ordinal) ? Kind.Command
        : path.Contains(Plugin, StringComparison.Ordinal) ? Kind.Plugin
        : path.StartsWith($"Features/{Module}/", StringComparison.Ordinal) || Path.GetFileName(path).Contains(Module, StringComparison.Ordinal) ? Kind.Module
        : Kind.App;

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
    //   the VSA.Sdk.Dev.props imports           become Sdk="VSA.Sdk", or <Sdk Name="VSA.Sdk" /> after another SDK
    //   a ProjectReference into framework/      becomes a PackageReference with the version of this tool
    internal static string ToPackageMode(string content, string version, string path) {
        if (content.Contains(DevProps, StringComparison.Ordinal) && !content.StartsWith("<Project>", StringComparison.Ordinal)) {
            content = content.Replace(DevProps, AddedSdk).Replace(DevTargets, "");
        } else if (content.Contains(DevProps, StringComparison.Ordinal)) {
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
    const string AddedSdk = "  <Sdk Name=\"VSA.Sdk\" />\n";

    [GeneratedRegex(@"^<Project Sdk=""VSA.Sdk"">\s*</Project>")]
    private static partial Regex Empty();

    [GeneratedRegex(@"<ProjectReference Include=""\$\(VsaFrameworkSourceDir\)[^""]*/(VSA\.[\w.]+)\.csproj"" />")]
    private static partial Regex SourceReference();
}
