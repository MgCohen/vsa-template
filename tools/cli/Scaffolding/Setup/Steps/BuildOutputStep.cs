namespace Umpire.Cli.Scaffolding.Setup.Steps;

using System.Xml.Linq;

sealed class BuildOutputStep : SetupStep {
    const string FileName = "Directory.Build.props";
    const string Root = "Project";
    const string ParentImport = "  <Import Project=\"$([MSBuild]::GetPathOfFileAbove("
        + "'Directory.Build.props', '$(MSBuildThisFileDirectory)../'))\" />\n";

    public override string Id => "build-output";

    public override SetupResult Plan(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.Read(FileName) is not string text) {
            return Change(
                FileName,
                context.ParentFile(FileName) is null ? template : XmlText.AddAtStart(template, Root, ParentImport));
        }
        if (XmlText.Parse(text) is not { } document) {
            return ByHand($"{FileName} is not valid XML. Add the PropertyGroup of the template to it.");
        }
        if (document.Descendants().Any(element => element.Name.LocalName == "UseArtifactsOutput")) {
            return Done();
        }
        var properties = XDocument.Parse(template).Root!.Element("PropertyGroup")!;
        return Change(FileName, XmlText.AddAtEnd(text, Root, XmlText.Lines(properties, "  ")));
    }
}
