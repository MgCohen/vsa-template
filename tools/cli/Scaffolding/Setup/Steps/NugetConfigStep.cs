namespace Umpire.Cli.Scaffolding.Setup.Steps;

sealed class NugetConfigStep : SetupStep {
    const string FileName = NugetConfig.FileName;
    const string Clear = "    <clear />\n";
    const string ByHandFeed = $"Add the umpire feed of the template to {FileName}.";

    public override string Id => "nuget-config";

    public override IReadOnlyList<string> Files(SetupContext context) => [FileName];

    public override SetupResult Plan(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.Read(FileName) is not string text) {
            var config = NugetConfig.Write(template, context.Sources);
            return Change(FileName, context.ParentFile(FileName) is null ? config : config.Replace(Clear, ""));
        }
        if (XmlText.Parse(text) is not { } document) {
            return ByHand($"{FileName} is not valid XML. {ByHandFeed}");
        }
        return NugetConfig.HasFeeds(document, context.Sources)
            ? Done()
            : ChangeOrByHand(FileName, NugetConfig.AddFeeds(text, document, template, context.Sources), ByHandFeed);
    }
}
