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
    public const string MsbuildSdks = "msbuild-sdks";
    const string GlobalJson = "global.json";
    const string CentralFile = "Directory.Packages.props";
    const string DevProps = "Umpire.Sdk.Dev.props";
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
        return [.. projects.Select(project => ProjectFile(project, central))];
    }

    public static IReadOnlyList<VersionSlot> InBin(string bin) =>
        Directory.Exists(bin)
            ? [.. Directory.GetFiles(bin, "*.deps.json").Order(StringComparer.Ordinal).SelectMany(PackagesOfDeps)]
            : [];

    public static bool HasUmpireMainSdk(string projectFile) {
        if (ParsedOrNull(projectFile)?.Root is not { } root) {
            return false;
        }
        return SdkPart.AllOf((string?)root.Attribute("Sdk")).Any(part => part.IsUmpire)
            || root.Elements().Any(element => element.Name.LocalName == "Import"
                && ((string?)element.Attribute("Project") ?? "").Contains(DevProps, StringComparison.Ordinal));
    }

    public static bool UsesUmpireSdk(string projectFile) =>
        ParsedOrNull(projectFile)?.Descendants().Any(NamesUmpireSdk) ?? false;

    public static IReadOnlyList<string> UnpinnedSdks(string root) {
        if (!Directory.Exists(root)) {
            return [];
        }
        var projects = ProjectFiles(root)
            .Where(file => SdkPart.AllOf((string?)ParsedOrNull(Path.Combine(root, file))?.Root?.Attribute("Sdk"))
                .Any(part => part.IsUmpire && part.Version is null))
            .Select(file => $"{file}: an Umpire SDK with no version. Write the version after the SDK name, "
                + $"for example Sdk=\"{Sdk}/<version>\"");
        var globalJson = FileAbove(root, GlobalJson) is string file && Json(file) is JsonObject json && PinsSdk(json)
            ? [$"{file}: an Umpire pin in {MsbuildSdks}. "
                + "Remove the entry, and write the version in each project file"]
            : Array.Empty<string>();
        return [.. globalJson, .. projects];
    }

    public static bool IsSdk(string? name) =>
        string.Equals(name, Sdk, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, WebSdk, StringComparison.OrdinalIgnoreCase);

    public static bool IsSdkElement(XElement element) =>
        element.Name.LocalName == "Sdk" && IsSdk((string?)element.Attribute("Name"));

    public static bool PinsSdk(JsonObject globalJson) =>
        globalJson[MsbuildSdks] is JsonObject sdks && sdks.Any(pair => IsSdk(pair.Key));

    public static IEnumerable<string> FoldersAbove(string start) {
        for (var folder = new DirectoryInfo(Path.GetFullPath(start)); folder is not null; folder = folder.Parent) {
            yield return folder.FullName;
        }
    }

    public static string? FileAbove(string start, string fileName) =>
        FoldersAbove(start).Select(folder => Path.Combine(folder, fileName)).FirstOrDefault(File.Exists);

    public static IReadOnlyList<string> ProjectFiles(string root) =>
        [.. Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.*.props", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/').Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal)];

    static bool NamesUmpireSdk(XElement element) =>
        IsSdkElement(element) || SdkPart.AllOf((string?)element.Attribute("Sdk")).Any(part => part.IsUmpire);

    static XDocument? ParsedOrNull(string file) {
        try {
            return XDocument.Load(file);
        } catch (XmlException) {
            return null;
        }
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

    static VersionFile ProjectFile(ProjectDocument project, IReadOnlyDictionary<string, string> central) {
        var slots = project.Document.Descendants()
            .SelectMany(element => SdkPins(project.File, element).Concat(Packages(project.File, element, central)))
            .ToList();
        return new VersionFile(project.File, slots, () => Text(project.Document));
    }

    static IEnumerable<VersionSlot> SdkPins(string file, XElement element) {
        if (IsSdkElement(element)) {
            if (element.Attribute("Version") is XAttribute version) {
                var name = (string)element.Attribute("Name")!;
                yield return new VersionSlot(file, name, version.Value, Line(element), value => version.Value = value);
            }
            yield break;
        }
        if (element.Attribute("Sdk") is not XAttribute sdk) {
            yield break;
        }
        foreach (var part in SdkPart.AllOf(sdk.Value).Where(part => part.IsUmpire && part.Version is not null)) {
            yield return new VersionSlot(
                file, part.Name, part.Version, Line(element), value => sdk.Value = WithSdk(sdk.Value, value));
        }
    }

    static string WithSdk(string sdks, string version) =>
        string.Join(';', sdks.Split(';').Select(part => SdkPart.Parse(part) is { IsUmpire: true, Version: not null } pin
            ? $"{pin.Name}/{version}"
            : part));

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

    static IEnumerable<XDocument> CentralAbove(string root) =>
        Directory.GetParent(Path.GetFullPath(root)) is { } parent
        && FileAbove(parent.FullName, CentralFile) is string file
            ? [XDocument.Load(file)]
            : [];

    static Dictionary<string, string> CentralVersions(IEnumerable<XDocument> documents) =>
        documents.SelectMany(document => document.Descendants())
            .Where(element => element.Name.LocalName == "PackageVersion" && Id(element) is not null)
            .Where(element => VersionAttribute(element) is not null)
            .GroupBy(element => Id(element)!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key, group => VersionAttribute(group.First())!.Value, StringComparer.OrdinalIgnoreCase);

    static string Text(XDocument document) {
        var settings = new XmlWriterSettings {
            OmitXmlDeclaration = document.Declaration is null,
            Encoding = new UTF8Encoding(false),
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace,
        };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings)) {
            document.Save(writer);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static bool IsVsa(string id) => id.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase);

    static bool IsPackage(XElement element) => element.Name.LocalName is "PackageReference" or "PackageVersion";

    static string? Id(XElement element) =>
        (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update");

    static XAttribute? VersionAttribute(XElement element) =>
        element.Attribute("VersionOverride") ?? element.Attribute("Version");

    static int? Line(XElement element) => element is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;

    sealed record ProjectDocument(string File, XDocument Document);
}
