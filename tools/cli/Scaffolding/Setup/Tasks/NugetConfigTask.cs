namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using Umpire.Files;

sealed class NugetConfigTask : SetupTask {
    const string FileName = NugetConfig.FileName;
    const string Clear = "    <clear />\n";
    const string ByHandFeed = $"Add the umpire feed of the template to {FileName}.";

    public override string Id => "nuget-config";

    public override SetupResult Plan(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.Read(FileName) is not string text) {
            var config = NugetConfig.Write(template, context.Sources);
            var kept = context.ParentFile(FileName) is null ? config : config.Replace(Clear, "");
            return new SetupResult.Change(new TextFile(FileName, kept));
        }
        if (XmlText.Parse(text) is not { } document) {
            return new SetupResult.ByHand($"{FileName} is not valid XML. {ByHandFeed}");
        }
        return NugetConfig.HasFeeds(document, context.Sources)
            ? new SetupResult.Done()
            : NugetConfig.AddFeeds(text, document, template, context.Sources) is string added
                ? new SetupResult.Change(new TextFile(FileName, added))
                : new SetupResult.ByHand(ByHandFeed);
    }
}
