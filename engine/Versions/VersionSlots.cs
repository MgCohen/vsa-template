namespace Umpire.Engine.Versions;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using Umpire.Architecture;

public static class VersionSlots {
    public const string CorePackage = "Umpire";
    public const string PackagePrefix = "Umpire.";
    public const string RemovedSdk = "Umpire.Sdk";
    public const string RemovedWebSdk = "Umpire.Sdk.Web";
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

    public static bool UsesUmpire(string projectFile) =>
        ParsedOrNull(projectFile)?.Descendants().Any(element => element.Name.LocalName == "PackageReference"
            && Id(element) is string id && IsUmpire(id)) ?? false;

    public static bool UsesRemovedSdk(string projectFile) =>
        ParsedOrNull(projectFile)?.Descendants().Any(NamesRemovedSdk) ?? false;

    public static IReadOnlyList<string> RemovedSdks(string root) {
        if (!Directory.Exists(root)) {
            return [];
        }
        var projects = ProjectFiles(root)
            .Where(file => UsesRemovedSdk(Path.Combine(root, file)))
            .Select(file => $"{file}: the Umpire SDK, which does not exist any more. Make the main SDK "
                + "Microsoft.NET.Sdk or Microsoft.NET.Sdk.Web, and add the Umpire packages as PackageReference items "
                + "('umpire init' does this for the root project)");
        var globalJson = GlobalJsonAbove(root) is string file
            && Json(file)?["msbuild-sdks"] is JsonObject sdks
            && sdks.Any(pair => IsRemovedSdk(pair.Key))
                ? [$"{file}: an Umpire SDK pin in msbuild-sdks. The Umpire SDK does not exist any more. Remove the entry"]
                : Array.Empty<string>();
        return [.. globalJson, .. projects];
    }

    public static bool IsRemovedSdk(string? name) =>
        string.Equals(name, RemovedSdk, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, RemovedWebSdk, StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<string> ProjectFiles(string root) =>
        [.. Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.*.props", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/')[..^1].Any(BuildOutput.IsIgnored))
            .Order(StringComparer.Ordinal)];

    static bool NamesRemovedSdk(XElement element) =>
        element.Name.LocalName == "Sdk"
            ? IsRemovedSdk((string?)element.Attribute("Name"))
            : element.Attribute("Sdk")?.Value.Split(';').Any(part => IsRemovedSdk(part.Split('/')[0].Trim())) ?? false;

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
            if (isPackage && key.Split('/') is [var id, var version] && IsUmpire(id)) {
                yield return new VersionSlot(Path.GetFileName(deps), id, version);
            }
        }
    }

    static VersionFile ProjectFile(string root, ProjectDocument project, IReadOnlyDictionary<string, string> central) {
        var slots = project.Document.Descendants()
            .SelectMany(element => Packages(project.File, element, central))
            .ToList();
        return new VersionFile(project.File, slots, () => Save(project.Document, Path.Combine(root, project.File)));
    }

    static IEnumerable<VersionSlot> Packages(
        string file, XElement element, IReadOnlyDictionary<string, string> central) {
        if (!IsPackage(element) || Id(element) is not string id || !IsUmpire(id)) {
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

    static bool IsUmpire(string id) =>
        string.Equals(id, CorePackage, StringComparison.OrdinalIgnoreCase)
        || id.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase);

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
