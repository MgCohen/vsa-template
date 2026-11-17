namespace Umpire.Engine.Versions;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;

public static class VersionSlots {
    public const string PackagePrefix = "Umpire.";
    public const string Sdk = "Umpire.Sdk";
    public const string WebSdk = "Umpire.Sdk.Web";
    const string CentralFile = "Directory.Packages.props";
    const LoadOptions KeepLayout = LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo;
    static readonly JsonDocumentOptions JsonAsDotnetReadsIt = new() {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static IReadOnlyList<VersionFile> InApp(string root) {
        if (!Directory.Exists(root)) {
            return [];
        }
        var projects = ProjectFiles(root)
            .Select(file => new ProjectDocument(file, XDocument.Load(Path.Combine(root, file), KeepLayout)))
            .ToList();
        var central = CentralVersions(projects.Select(project => project.Document).Concat(CentralAbove(root)));
        return [.. projects.Select(project => ProjectFile(root, project, central))];
    }

    public static IReadOnlyList<VersionSlot> InBin(string bin) =>
        Directory.Exists(bin)
            ? [.. Directory.GetFiles(bin, "*.deps.json").Order(StringComparer.Ordinal).SelectMany(PackagesOfDeps)]
            : [];

    public static bool UsesUmpireSdk(string projectFile) =>
        ParsedOrNull(projectFile)?.Descendants().Any(NamesUmpireSdk) ?? false;

    public static IReadOnlyList<string> UnpinnedSdks(string root) {
        if (!Directory.Exists(root)) {
            return [];
        }
        var projects = ProjectFiles(root)
            .Where(file => ParsedOrNull(Path.Combine(root, file))?.Root?.Attribute("Sdk") is XAttribute sdk
                && sdk.Value.Split(';', StringSplitOptions.TrimEntries).Any(IsSdk))
            .Select(file => $"{file}: an Umpire SDK with no version. Write the version after the SDK name, "
                + $"for example Sdk=\"{Sdk}/<version>\"");
        var globalJson = GlobalJsonAbove(root) is string file
            && Json(file)?["msbuild-sdks"] is JsonObject sdks
            && sdks.Any(pair => IsSdk(pair.Key))
                ? [$"{file}: an Umpire pin in msbuild-sdks. "
                    + "Remove the entry, and write the version in each project file"]
                : Array.Empty<string>();
        return [.. globalJson, .. projects];
    }

    public static bool IsSdk(string? name) =>
        string.Equals(name, Sdk, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, WebSdk, StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> ProjectFiles(string root) =>
        [.. Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.*.props", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/').Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)];

    static bool NamesUmpireSdk(XElement element) =>
        element.Name.LocalName == "Sdk"
            ? IsSdk((string?)element.Attribute("Name"))
            : element.Attribute("Sdk")?.Value.Split(';').Any(part => IsSdk(part.Split('/')[0].Trim())) ?? false;

    static XDocument? ParsedOrNull(string file) {
        try {
            return XDocument.Load(file);
        } catch (XmlException) {
            return null;
        }
    }

    static string? GlobalJsonAbove(string root) {
        for (var folder = new DirectoryInfo(Path.GetFullPath(root)); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, "global.json");
            if (File.Exists(file)) {
                return file;
            }
        }
        return null;
    }

    static JsonNode? Json(string file) => JsonNode.Parse(File.ReadAllText(file), documentOptions: JsonAsDotnetReadsIt);

    static IEnumerable<VersionSlot> PackagesOfDeps(string deps) {
        if (Json(deps)?["libraries"] is not JsonObject libraries) {
            yield break;
        }
        foreach (var (key, library) in libraries) {
            var isPackage = library?["type"]?.GetValue<string>() == "package";
            if (isPackage && key.Split('/') is [var id, var version] && IsVsa(id)) {
                yield return new VersionSlot(Path.GetFileName(deps), id, version);
            }
        }
    }

    static VersionFile ProjectFile(string root, ProjectDocument project, IReadOnlyDictionary<string, string> central) {
        var slots = project.Document.Descendants()
            .SelectMany(element => SdkPins(project.File, element).Concat(Packages(project.File, element, central)))
            .ToList();
        return new VersionFile(project.File, slots, () => Save(project.Document, Path.Combine(root, project.File)));
    }

    static IEnumerable<VersionSlot> SdkPins(string file, XElement element) {
        if (element.Name.LocalName == "Sdk") {
            var name = (string?)element.Attribute("Name");
            if (IsSdk(name) && element.Attribute("Version") is XAttribute version) {
                yield return new VersionSlot(file, name!, version.Value, Line(element), value => version.Value = value);
            }
            yield break;
        }
        if (element.Attribute("Sdk") is not XAttribute sdk) {
            yield break;
        }
        var parts = sdk.Value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts) {
            if (part.Split('/') is [var name, var version] && IsSdk(name)) {
                yield return new VersionSlot(
                    file, name, version, Line(element), value => sdk.Value = WithSdk(sdk.Value, value));
            }
        }
    }

    static string WithSdk(string sdks, string version) =>
        string.Join(';', sdks.Split(';').Select(part => SdkPinName(part) is string name ? $"{name}/{version}" : part));

    static string? SdkPinName(string part) => part.Trim().Split('/') is [var name, _] && IsSdk(name) ? name : null;

    static IEnumerable<VersionSlot> Packages(
        string file, XElement element, IReadOnlyDictionary<string, string> central) {
        if (!IsPackage(element) || Id(element) is not string id || !IsVsa(id)) {
            yield break;
        }
        if (VersionAttribute(element) is XAttribute version) {
            yield return new VersionSlot(file, id, version.Value, Line(element), value => version.Value = value);
        } else if (element.Name.LocalName == "PackageReference" && element.Attribute("Include") is not null) {
            yield return new VersionSlot(file, id, central.GetValueOrDefault(id), Line(element));
        }
    }

    static IEnumerable<XDocument> CentralAbove(string root) {
        for (var folder = Directory.GetParent(Path.GetFullPath(root)); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, CentralFile);
            if (File.Exists(file)) {
                return [XDocument.Load(file)];
            }
        }
        return [];
    }

    static Dictionary<string, string> CentralVersions(IEnumerable<XDocument> documents) =>
        documents.SelectMany(document => document.Descendants())
            .Where(element => element.Name.LocalName == "PackageVersion" && Id(element) is not null)
            .Where(element => VersionAttribute(element) is not null)
            .GroupBy(element => Id(element)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key, group => VersionAttribute(group.First())!.Value, StringComparer.OrdinalIgnoreCase);

    static void Save(XDocument document, string path) {
        var content = TextContent.Read(path);
        var settings = new XmlWriterSettings {
            OmitXmlDeclaration = document.Declaration is null,
            Encoding = new UTF8Encoding(content.Bom),
            NewLineChars = content.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n",
            NewLineHandling = NewLineHandling.Replace,
        };
        using var writer = XmlWriter.Create(path, settings);
        document.Save(writer);
    }

    static bool IsVsa(string id) => id.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase);

    static bool IsPackage(XElement element) => element.Name.LocalName is "PackageReference" or "PackageVersion";

    static string? Id(XElement element) =>
        (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update");

    static XAttribute? VersionAttribute(XElement element) =>
        element.Attribute("VersionOverride") ?? element.Attribute("Version");

    static int? Line(XElement element) => element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;

    sealed record ProjectDocument(string File, XDocument Document);

    sealed record TextContent(string Text, bool Bom) {
        public static TextContent Read(string path) =>
            new(File.ReadAllText(path), File.ReadAllBytes(path) is [0xEF, 0xBB, 0xBF, ..]);
    }
}
