using System.Text.Json.Nodes;
using System.Xml.Linq;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa doctor: the tool, VSA.Sdk and the VSA.* packages ship together, so the app must use one version of all.
// It reads the running tool, dotnet-tools.json, global.json and each VSA.* package reference.
static class DoctorCommand {
    public static int Run() {
        var root = AppRoot.Find();
        var found = new List<(string Where, string What, string? Version)> {
            ("this tool", "VSA.Cli", ToolVersion.Current),
            ("dotnet-tools.json", "vsa.cli", ToolManifestVersion(Path.Combine(root, "dotnet-tools.json"))),
            ("global.json", "VSA.Sdk", AppRoot.SdkVersion(Path.Combine(root, "global.json"))),
        };
        found.AddRange(PackageVersions(root));

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

    static string? ToolManifestVersion(string file) =>
        File.Exists(file) ? (JsonNode.Parse(File.ReadAllText(file))?["tools"]?["vsa.cli"]?["version"]?.GetValue<string>()) : null;

    // Each VSA.* PackageReference or PackageVersion in the project files of the app, without bin/ and obj/.
    static IEnumerable<(string, string, string?)> PackageVersions(string root) {
        var files = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.*.props", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/').Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal);
        foreach (var file in files) {
            var items = XDocument.Load(Path.Combine(root, file)).Descendants()
                .Where(e => e.Name.LocalName is "PackageReference" or "PackageVersion");
            foreach (var item in items) {
                var id = (string?)item.Attribute("Include") ?? (string?)item.Attribute("Update") ?? "";
                if (id.StartsWith("VSA.", StringComparison.OrdinalIgnoreCase)) {
                    yield return (file, id, (string?)item.Attribute("VersionOverride") ?? (string?)item.Attribute("Version"));
                }
            }
        }
    }
}
