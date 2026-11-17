namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using Umpire.Engine.Versions;

sealed class UmpireSdkTask : SetupTask {
    const string Root = "Project";
    const string SdkElement = "<Sdk Name=\"Umpire.Sdk\" />";

    public override string Id => "umpire-sdk";

    public override IReadOnlyList<string> Files(SetupContext context) => [context.RootProject];

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        var byHand = $"Add {SdkElement} to {project}.";
        if (context.Read(project) is not string text || XmlText.Parse(text)?.Root is not { } root) {
            return ByHand($"{project} is not valid XML. {byHand}");
        }
        var inAttribute = ((string?)root.Attribute("Sdk") ?? "")
            .Split(';', StringSplitOptions.TrimEntries)
            .Any(sdk => IsUmpireSdk(sdk.Split('/')[0]));
        var inElement = root.Elements().Any(element =>
            element.Name.LocalName == "Sdk" && IsUmpireSdk((string?)element.Attribute("Name")));
        if (inAttribute || inElement) {
            return Done();
        }
        var line = $"{XmlText.Indent(text, Root)}{SdkElement}\n";
        return ChangeOrByHand(project, XmlText.AddAtStart(text, Root, line), byHand);
    }

    static bool IsUmpireSdk(string? name) => string.Equals(name, VersionSlots.Sdk, StringComparison.OrdinalIgnoreCase);
}
