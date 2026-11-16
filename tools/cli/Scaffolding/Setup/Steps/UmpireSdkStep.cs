namespace Umpire.Cli.Scaffolding.Setup.Steps;

using Umpire.Engine.Versions;

sealed class UmpireSdkStep : SetupStep {
    const string SdkElement = "  <Sdk Name=\"Umpire.Sdk\" />\n";

    public override string Id => "umpire-sdk";

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        if (context.Read(project) is not string text || XmlText.Parse(text)?.Root is not { } root) {
            return ByHand($"{project} is not valid XML. Add {SdkElement.Trim()} to it.");
        }
        var inAttribute = ((string?)root.Attribute("Sdk") ?? "")
            .Split(';', StringSplitOptions.TrimEntries)
            .Any(sdk => IsUmpireSdk(sdk.Split('/')[0]));
        var inElement = root.Elements().Any(element =>
            element.Name.LocalName == "Sdk" && IsUmpireSdk((string?)element.Attribute("Name")));
        return inAttribute || inElement ? Done() : Change(project, XmlText.AddAtStart(text, "Project", SdkElement));
    }

    static bool IsUmpireSdk(string? name) => string.Equals(name, VersionSlots.Sdk, StringComparison.OrdinalIgnoreCase);
}
