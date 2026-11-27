namespace Umpire.Cli.Features.Scaffold.Domain.Setup.Tasks;

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Umpire.Engine.Versions;
using Umpire.Files;

sealed partial class UmpirePackagesTask : SetupTask {
    const string NetSdk = "Microsoft.NET.Sdk";
    const string NetWebSdk = "Microsoft.NET.Sdk.Web";
    const string WebPackage = "Umpire.Web";
    const string VsaPackage = "Umpire.Vsa";
    const string EnginePackage = "Umpire.Engine";
    const string DefaultTarget = "net10.0";
    static readonly string[] centralFiles = ["Directory.Packages.props", "Directory.Build.props"];

    public override string Id => "umpire-packages";

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        var first = $"<Project Sdk=\"{NetSdk}\">, or <Project Sdk=\"{NetWebSdk}\"> for a web app";
        if (context.Read(project) is not string text || XmlText.Parse(text)?.Root is not { } root) {
            return new SetupResult.ByHand($"{project} is not valid XML. Make its first line {first}.");
        }
        if (root.Elements().Any(IsRemovedSdkElement)) {
            return new SetupResult.ByHand($"{project} adds the Umpire SDK with an <Sdk> element. "
                + "The Umpire SDK does not exist any more. Remove the <Sdk> element.");
        }
        if ((string?)root.Attribute("Sdk") is not string sdk) {
            return new SetupResult.ByHand($"{project} has no Sdk attribute. Make its first line {first}.");
        }
        var given = sdk.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var parts = given.Select(NetSdkOf).ToList();
        var changed = text;
        if (parts.Any(part => part is null)) {
            return new SetupResult.ByHand($"The Sdk of {project} is not valid. Make its first line {first}.");
        }
        if (!parts.SequenceEqual(given)) {
            var main = string.Join(';', parts);
            if (RootSdk().Match(text).Groups["value"] is not { Success: true } value) {
                return new SetupResult.ByHand($"The tool cannot change the Sdk attribute of {project}. "
                    + $"Make its first line {first}.");
            }
            changed = text[..value.Index] + main + text[(value.Index + value.Length)..];
            if (!root.Descendants().Any(IsTarget)) {
                var indent = XmlText.Indent(changed, "Project");
                var settings = XmlText.Lines(GeneralSettings(), indent, indent);
                changed = XmlText.AddAtStart(changed, "Project", settings) ?? changed;
            }
        }
        changed = WithPackages(context, root, changed, parts.Contains(NetWebSdk));
        return changed == text ? new SetupResult.Done() : new SetupResult.Change(new TextFile(project, changed));
    }

    static string WithPackages(SetupContext context, XElement root, string text, bool web) {
        var missing = MissingPackages(root, web);
        if (missing is []) {
            return text;
        }
        var central = UsesCentralVersions(context);
        var group = new XElement("ItemGroup", missing.Select(id => Reference(id, context.Version, central)));
        var indent = XmlText.Indent(text, "Project");
        return XmlText.AddAtEnd(text, "Project", XmlText.Lines(group, indent, indent)) ?? text;
    }

    static string? NetSdkOf(string part) {
        var name = part.Split('/', StringSplitOptions.TrimEntries)[0];
        return string.Equals(name, VersionSlots.RemovedWebSdk, StringComparison.OrdinalIgnoreCase) ? NetWebSdk
            : string.Equals(name, VersionSlots.RemovedSdk, StringComparison.OrdinalIgnoreCase) ? NetSdk
            : name.Length == 0 ? null
            : part;
    }

    static IReadOnlyList<string> MissingPackages(XElement root, bool web) {
        var ids = root.Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? "")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasPack = ids.Contains(VersionSlots.CorePackage) || ids.Contains(WebPackage)
            || (!web && ids.Contains(VsaPackage));
        return [
            .. hasPack ? Array.Empty<string>() : [web ? WebPackage : VsaPackage],
            .. ids.Contains(EnginePackage) ? Array.Empty<string>() : [EnginePackage],
        ];
    }

    static bool IsTarget(XElement element) => element.Name.LocalName is "TargetFramework" or "TargetFrameworks";

    static XElement GeneralSettings() => new("PropertyGroup",
        new XElement("TargetFramework", DefaultTarget),
        new XElement("Nullable", "enable"),
        new XElement("ImplicitUsings", "enable"));

    static XElement Reference(string id, string version, bool central) {
        var reference = new XElement("PackageReference", new XAttribute("Include", id),
            new XAttribute(central ? "VersionOverride" : "Version", version));
        if (id == EnginePackage) {
            reference.Add(new XAttribute("PrivateAssets", "all"));
        }
        return reference;
    }

    static bool UsesCentralVersions(SetupContext context) =>
        centralFiles.Any(file => context.Read(file) is string text && CentralVersions.On(text))
        || CentralVersions.Above(Path.GetDirectoryName(Path.GetFullPath(context.Block))!);

    static bool IsRemovedSdkElement(XElement element) =>
        element.Name.LocalName == "Sdk" && VersionSlots.IsRemovedSdk((string?)element.Attribute("Name"));

    [GeneratedRegex("""<Project\b[^>]*?\sSdk\s*=\s*(?<quote>["'])(?<value>[^"']*)\k<quote>""")]
    private static partial Regex RootSdk();
}
