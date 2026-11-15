using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

namespace VSA.Architecture.Versions;

public static class VersionSlots {
    public const string PackagePrefix = "VSA.";
    public const string Sdk = "VSA.Sdk";
    public const string Tool = "vsa.cli";
    public const string GlobalJsonFile = "global.json";
    const LoadOptions KeepLayout = LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo;
    static readonly string[] ManifestFiles = ["dotnet-tools.json", ".config/dotnet-tools.json"];
    static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static IReadOnlyList<VersionFile> InApp(string root) {
        if (!Directory.Exists(root)) {
            return [];
        }
        var projects = ProjectFiles(root)
            .Select(file => new ProjectDocument(file, XDocument.Load(Path.Combine(root, file), KeepLayout)))
            .ToList();
        var central = CentralVersions(projects);
        var files = new List<VersionFile>();
        files.AddRange(ToolManifests(root).Select(manifest => ToolManifest(root, manifest)));
        if (GlobalJson(root) is VersionFile globalJson) {
            files.Add(globalJson);
        }
        files.AddRange(projects.Select(project => ProjectFile(root, project, central)));
        return files;
    }

    public static IReadOnlyList<VersionSlot> InBin(string bin) =>
        Directory.Exists(bin)
            ? [.. Directory.GetFiles(bin, PackagePrefix + "*.dll").Order(StringComparer.Ordinal).Select(BinSlot)]
            : [];

    public static IReadOnlyList<string> ToolManifests(string root) =>
        [.. ManifestFiles.Where(manifest => File.Exists(Path.Combine(root, manifest)))];

    public static string? ManifestToolVersion(string manifestFile) =>
        JsonNode.Parse(File.ReadAllText(manifestFile))?["tools"]?[Tool]?["version"]?.GetValue<string>();

    public static string? SdkVersion(string globalJsonFile) =>
        (JsonNode.Parse(File.ReadAllText(globalJsonFile)) as JsonObject)?["msbuild-sdks"]?[Sdk]?.GetValue<string>();

    public static IReadOnlyList<string> ProjectFiles(string root) =>
        [.. Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.*.props", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/').Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)];

    static VersionSlot BinSlot(string dll) =>
        new(Path.GetFileName(dll), Path.GetFileNameWithoutExtension(dll), PackageVersion.Of(dll));

    static VersionFile ToolManifest(string root, string manifest) {
        var version = ManifestToolVersion(Path.Combine(root, manifest));
        VersionSlot[] slots = version is null ? [] : [new VersionSlot(manifest, Tool, version)];
        return new VersionFile(manifest, slots, () => { });
    }

    static VersionFile? GlobalJson(string root) {
        var path = Path.Combine(root, GlobalJsonFile);
        if (!File.Exists(path)) {
            return null;
        }
        var json = JsonNode.Parse(File.ReadAllText(path));
        if (json?["msbuild-sdks"] is not JsonObject sdks || sdks[Sdk]?.GetValue<string>() is not string version) {
            return null;
        }
        var slot = new VersionSlot(GlobalJsonFile, Sdk, version, write: value => sdks[Sdk] = value);
        var text = () => json.ToJsonString(Indented) + Environment.NewLine;
        return new VersionFile(GlobalJsonFile, [slot], () => File.WriteAllText(path, text()));
    }

    static VersionFile ProjectFile(string root, ProjectDocument project, IReadOnlyDictionary<string, string> central) {
        var slots = project.Document.Descendants()
            .SelectMany(element => SdkPins(project.File, element).Concat(Packages(project.File, element, central)))
            .ToList();
        return new VersionFile(project.File, slots, () => Save(project.Document, Path.Combine(root, project.File)));
    }

    static IEnumerable<VersionSlot> SdkPins(string file, XElement element) {
        if (element.Name.LocalName == "Sdk") {
            if (IsSdk((string?)element.Attribute("Name")) && element.Attribute("Version") is XAttribute version) {
                yield return new VersionSlot(file, Sdk, version.Value, Line(element), value => version.Value = value);
            }
            yield break;
        }
        if (element.Attribute("Sdk") is not XAttribute sdk) {
            yield break;
        }
        foreach (var part in sdk.Value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            if (part.Split('/') is [var name, var version] && IsSdk(name)) {
                yield return new VersionSlot(file, Sdk, version, Line(element), value => sdk.Value = WithSdk(sdk.Value, value));
            }
        }
    }

    static string WithSdk(string sdks, string version) =>
        string.Join(';', sdks.Split(';').Select(part => SdkPinName(part) is string name ? $"{name}/{version}" : part));

    static string? SdkPinName(string part) => part.Trim().Split('/') is [var name, _] && IsSdk(name) ? name : null;

    static IEnumerable<VersionSlot> Packages(string file, XElement element, IReadOnlyDictionary<string, string> central) {
        if (!IsPackage(element) || Id(element) is not string id || !IsVsa(id)) {
            yield break;
        }
        if (VersionAttribute(element) is XAttribute version) {
            yield return new VersionSlot(file, id, version.Value, Line(element), value => version.Value = value);
        } else if (central.TryGetValue(id, out var centralVersion)) {
            yield return new VersionSlot(file, id, centralVersion, Line(element));
        }
    }

    static Dictionary<string, string> CentralVersions(IEnumerable<ProjectDocument> projects) =>
        projects.SelectMany(project => project.Document.Descendants())
            .Where(element => element.Name.LocalName == "PackageVersion" && Id(element) is not null)
            .Where(element => VersionAttribute(element) is not null)
            .GroupBy(element => Id(element)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => VersionAttribute(group.First())!.Value, StringComparer.OrdinalIgnoreCase);

    static void Save(XDocument document, string path) {
        var bom = File.ReadAllBytes(path) is [0xEF, 0xBB, 0xBF, ..];
        var settings = new XmlWriterSettings {
            OmitXmlDeclaration = document.Declaration is null,
            Encoding = new UTF8Encoding(bom),
        };
        using var writer = XmlWriter.Create(path, settings);
        document.Save(writer);
    }

    static bool IsSdk(string? name) => string.Equals(name, Sdk, StringComparison.OrdinalIgnoreCase);

    static bool IsVsa(string id) => id.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase);

    static bool IsPackage(XElement element) => element.Name.LocalName is "PackageReference" or "PackageVersion";

    static string? Id(XElement element) => (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update");

    static XAttribute? VersionAttribute(XElement element) =>
        element.Attribute("VersionOverride") ?? element.Attribute("Version");

    static int? Line(XElement element) => element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;

    sealed record ProjectDocument(string File, XDocument Document);
}
