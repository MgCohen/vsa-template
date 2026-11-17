namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using System.Text.RegularExpressions;
using Umpire.Engine.Versions;
using Umpire.Files;

sealed partial class UmpireSdkTask : SetupTask {
    const string NetSdk = "Microsoft.NET.Sdk";
    const string NetWebSdk = "Microsoft.NET.Sdk.Web";

    public override string Id => "umpire-sdk";

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        var version = context.Version;
        var main = $"<Project Sdk=\"{VersionSlots.Sdk}/{version}\">, "
            + $"or <Project Sdk=\"{VersionSlots.WebSdk}/{version}\"> for a web app";
        if (context.Read(project) is not string text || XmlText.Parse(text)?.Root is not { } root) {
            return new SetupResult.ByHand($"{project} is not valid XML. Make its first line {main}.");
        }
        if (root.Elements().Any(VersionSlots.IsSdkElement)) {
            return new SetupResult.ByHand($"{project} adds the Umpire SDK with an <Sdk> element. "
                + "An Umpire SDK must be the main SDK, "
                + $"because it sets the output folder before the platform SDK. Remove the <Sdk> element, "
                + $"and make the first line {main}.");
        }
        if ((string?)root.Attribute("Sdk") is not string sdk) {
            return new SetupResult.ByHand($"{project} has no Sdk attribute. Make its first line {main}.");
        }
        var parts = SdkPart.AllOf(sdk);
        var umpire = parts.Where(part => part.IsUmpire).ToList();
        if (umpire.Count > 0) {
            return umpire switch {
                [{ Version: null } only] => Changed(project, text, string.Join(';', parts.Select(
                    part => part == only ? only with { Version = version } : part)), main),
                [{ Version: var pinned }] when pinned == version => new SetupResult.Done(),
                [{ Name: var name, Version: var pinned }] => new SetupResult.ByHand(
                    $"{project} pins {name} {pinned}, and the tool is {version}. Run 'umpire update'."),
                _ => new SetupResult.ByHand($"The Sdk of {project} is not valid. Make its first line {main}."),
            };
        }
        return parts switch {
            [{ Name: NetSdk, Version: null }] => Changed(project, text, $"{VersionSlots.Sdk}/{version}", main),
            [{ Name: NetWebSdk, Version: null }] => Changed(project, text, $"{VersionSlots.WebSdk}/{version}", main),
            _ => new SetupResult.ByHand($"{project} has the main SDK \"{sdk}\". An Umpire SDK must be the main SDK. "
                + $"Make the first line {main}."),
        };
    }

    static SetupResult Changed(string project, string text, string sdk, string main) {
        var value = RootSdk().Match(text).Groups["value"];
        var changed = value.Success ? text[..value.Index] + sdk + text[(value.Index + value.Length)..] : null;
        return changed is not null
            ? new SetupResult.Change(new TextFile(project, changed))
            : new SetupResult.ByHand($"The tool cannot change the Sdk attribute of {project}. "
                + $"Make its first line {main}.");
    }

    [GeneratedRegex("""<Project\b[^>]*?\sSdk\s*=\s*(?<quote>["'])(?<value>[^"']*)\k<quote>""")]
    private static partial Regex RootSdk();
}
