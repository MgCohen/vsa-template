using System.Text.Json.Nodes;
using System.Xml.Linq;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa doctor: the tool, VSA.Sdk and the VSA.* packages ship together, so the app must use one version of all.
// It reads the running tool, the tool manifests, global.json, each VSA.Sdk pin and each VSA.* package reference.
static class DoctorCommand {
    public static int Run() {
        var found = Versions(AppRoot.Find());

        var width = found.Max(f => f.Where.Length);
        foreach (var (where, what, version) in found) {
            Console.WriteLine($"  {where.PadRight(width)}  {what,-20} {version ?? "(missing)"}");
        }
        var versions = found.Select(f => f.Version).Distinct().ToList();
        if (versions is [string one]) {
            Console.WriteLine($"OK: all use VSA {one}.");
            return 0;
        }
        throw new VsaException("The versions are not the same. Use one version for the tool, VSA.Sdk and all VSA.* packages.");
    }

    // Each place in the app that gives a VSA version. A null version is a place that must give one, but does not.
    internal static List<(string Where, string What, string? Version)> Versions(string root) {
        var found = new List<(string, string, string?)> { ("this tool", "VSA.Cli", ToolVersion.Current) };
        // dotnet new tool-manifest writes .config/dotnet-tools.json. vsa new app writes dotnet-tools.json.
        var manifests = new[] { "dotnet-tools.json", ".config/dotnet-tools.json" }.Where(m => File.Exists(Path.Combine(root, m))).ToList();
        found.AddRange(manifests is []
            ? [("dotnet-tools.json", "vsa.cli", null)]
            : manifests.Select(m => (m, "vsa.cli", ToolManifestVersion(Path.Combine(root, m)))));
        found.Add(("global.json", "VSA.Sdk", AppRoot.SdkVersion(Path.Combine(root, "global.json"))));
        found.AddRange(ProjectVersions(root));
        return found;
    }

    static string? ToolManifestVersion(string file) =>
        JsonNode.Parse(File.ReadAllText(file))?["tools"]?["vsa.cli"]?["version"]?.GetValue<string>();

    // In each project file of the app, without bin/ and obj/:
    //   a VSA.Sdk pin        <Project Sdk="VSA.Sdk/1.0.0">, <Import Sdk="VSA.Sdk/1.0.0" />, <Sdk Name="VSA.Sdk" Version="1.0.0" />
    //   a VSA.* package      PackageReference or PackageVersion, with Version or VersionOverride. A PackageReference with
    //                        no version gets it from a PackageVersion (central package management).
    static IEnumerable<(string, string, string?)> ProjectVersions(string root) {
        var files = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.*.props", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/').Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)
            .Select(file => (Path: file, Elements: XDocument.Load(Path.Combine(root, file)).Descendants().ToList()))
            .ToList();
        var central = files.SelectMany(f => f.Elements)
            .Where(e => e.Name.LocalName == "PackageVersion" && Id(e) is not null && Version(e) is not null)
            .GroupBy(e => Id(e)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => Version(g.First()), StringComparer.OrdinalIgnoreCase);

        foreach (var (file, elements) in files) {
            foreach (var element in elements) {
                foreach (var pin in SdkPins(element)) {
                    yield return (file, "VSA.Sdk", pin);
                }
                var id = Id(element);
                if (element.Name.LocalName is "PackageReference" or "PackageVersion" && id is not null && id.StartsWith("VSA.", StringComparison.OrdinalIgnoreCase)) {
                    yield return (file, id, Version(element) ?? central.GetValueOrDefault(id));
                }
            }
        }
    }

    // The VSA.Sdk versions that this element pins. An Sdk attribute can name more than one SDK, separated by ";".
    static IEnumerable<string> SdkPins(XElement element) {
        if (element.Name.LocalName == "Sdk") {
            if (string.Equals((string?)element.Attribute("Name"), "VSA.Sdk", StringComparison.OrdinalIgnoreCase) && (string?)element.Attribute("Version") is string version) {
                yield return version;
            }
            yield break;
        }
        foreach (var sdk in ((string?)element.Attribute("Sdk") ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            if (sdk.Split('/') is [var name, var version] && string.Equals(name, "VSA.Sdk", StringComparison.OrdinalIgnoreCase)) {
                yield return version;
            }
        }
    }

    static string? Id(XElement e) => (string?)e.Attribute("Include") ?? (string?)e.Attribute("Update");

    static string? Version(XElement e) => (string?)e.Attribute("VersionOverride") ?? (string?)e.Attribute("Version");
}
