namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using System.Xml.Linq;

sealed class BuildOutputTask : SetupTask {
    const string FileName = "Directory.Build.props";
    const string Root = "Project";
    const string ByHandProperties = $"Add the PropertyGroup of the template to {FileName}.";
    const string ParentImport = "  <Import Project=\"$([MSBuild]::GetPathOfFileAbove("
        + "'Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" />\n";

    public override string Id => "build-output";

    public override IReadOnlyList<string> Files(SetupContext context) => [FileName];

    public override SetupResult Plan(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.Read(FileName) is not string text) {
            return Change(
                FileName,
                context.ParentFile(FileName) is null ? template : XmlText.AddAtStart(template, Root, ParentImport)!);
        }
        if (XmlText.Parse(text) is not { } document) {
            return ByHand($"{FileName} is not valid XML. {ByHandProperties}");
        }
        if (document.Descendants().Any(element => element.Name.LocalName == "UseArtifactsOutput")) {
            return Done();
        }
        var unit = XmlText.Indent(text, Root);
        var properties = XDocument.Parse(template).Root!.Element("PropertyGroup")!;
        return ChangeOrByHand(
            FileName, XmlText.AddAtEnd(text, Root, XmlText.Lines(properties, unit, unit)), ByHandProperties);
    }
}
