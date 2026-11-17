namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using Umpire.Engine.Versions;

sealed class UmpireSdkTask : SetupTask {
    const string NetSdk = "Microsoft.NET.Sdk";
    const string NetWebSdk = "Microsoft.NET.Sdk.Web";

    public override string Id => "umpire-sdk";

    public override IReadOnlyList<string> Files(SetupContext context) => [context.RootProject];

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        var version = context.Version;
        var main = $"<Project Sdk=\"{VersionSlots.Sdk}/{version}\">, "
            + $"or <Project Sdk=\"{VersionSlots.WebSdk}/{version}\"> for a web app";
        if (context.Read(project) is not string text || XmlText.Parse(text)?.Root is not { } root) {
            return ByHand($"{project} is not valid XML. Make its first line {main}.");
        }
        if (root.Elements().Any(IsUmpireSdkElement)) {
            return ByHand($"{project} adds the Umpire SDK with an <Sdk> element. An Umpire SDK must be the main SDK, "
                + $"because it sets the output folder before the platform SDK. Remove the <Sdk> element, "
                + $"and make the first line {main}.");
        }
        var sdk = (string?)root.Attribute("Sdk") ?? "";
        var parts = sdk.Split('/', StringSplitOptions.TrimEntries);
        if (VersionSlots.IsSdk(parts[0])) {
            return parts switch {
                [_] => Change(project, WithSdk(text, sdk, $"{parts[0]}/{version}")),
                [_, var pinned] when pinned == version => Done(),
                [_, var pinned] => ByHand(
                    $"{project} pins {parts[0]} {pinned}, and the tool is {version}. Run 'umpire update'."),
                _ => ByHand($"The Sdk of {project} is not valid. Make its first line {main}."),
            };
        }
        return sdk switch {
            NetSdk => Change(project, WithSdk(text, sdk, $"{VersionSlots.Sdk}/{version}")),
            NetWebSdk => Change(project, WithSdk(text, sdk, $"{VersionSlots.WebSdk}/{version}")),
            _ => ByHand($"{project} has the main SDK \"{sdk}\". An Umpire SDK must be the main SDK. "
                + $"Make the first line {main}."),
        };
    }

    static bool IsUmpireSdkElement(System.Xml.Linq.XElement element) =>
        element.Name.LocalName == "Sdk" && VersionSlots.IsSdk((string?)element.Attribute("Name"));

    static string WithSdk(string text, string sdk, string umpireSdk) {
        var attribute = $"Sdk=\"{sdk}\"";
        var start = text.IndexOf(attribute, StringComparison.Ordinal);
        return text[..start] + $"Sdk=\"{umpireSdk}\"" + text[(start + attribute.Length)..];
    }
}
