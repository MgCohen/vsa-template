namespace Umpire.Cli.Scaffolding.Setup.Tasks;

sealed class BuildPackageTask : SetupTask {
    const string Package = "Umpire.Engine.Build";
    const string Root = "Project";
    const string CentralFile = "Directory.Packages.props";
    const string CentralSwitch = "ManagePackageVersionsCentrally";
    static readonly string[] SwitchFiles = [CentralFile, "Directory.Build.props"];

    public override string Id => "build-package";

    public override IReadOnlyList<string> Files(SetupContext context) => [context.RootProject];

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        var version = context.Version;
        var byHand = $"Add the package {Package} {version} with PrivateAssets=\"all\" to {project}.";
        if (context.Read(project) is not string text || XmlText.Parse(text) is not { } document) {
            return ByHand($"{project} is not valid XML. {byHand}");
        }
        var hasPackage = document.Descendants().Any(element =>
            element.Name.LocalName == "PackageReference"
            && string.Equals(
                (string?)element.Attribute("Include") ?? (string?)element.Attribute("Update"),
                Package,
                StringComparison.OrdinalIgnoreCase));
        if (hasPackage) {
            return Done();
        }
        if (UsesCentralVersions(context, text)) {
            return ByHand($"The app uses central package versions. Add <PackageReference Include=\"{Package}\" "
                + $"PrivateAssets=\"all\" /> to {project}, and <PackageVersion Include=\"{Package}\" "
                + $"Version=\"{version}\" /> to {CentralFile}.");
        }
        var unit = XmlText.Indent(text, Root);
        var reference = $"{unit}<ItemGroup>\n{unit}{unit}<PackageReference Include=\"{Package}\" Version=\"{version}\" "
            + $"PrivateAssets=\"all\" />\n{unit}</ItemGroup>\n";
        return ChangeOrByHand(project, XmlText.AddAtEnd(text, Root, reference), byHand);
    }

    static bool UsesCentralVersions(SetupContext context, string project) {
        IEnumerable<string?> texts = [
            project,
            .. SwitchFiles.SelectMany(file => new[] {
                context.Read(file),
                context.ParentFile(file) is string parent ? File.ReadAllText(parent) : null,
            }),
        ];
        return texts.OfType<string>().Any(text =>
            XmlText.Parse(text)?.Descendants().Any(element =>
                element.Name.LocalName == CentralSwitch
                && string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase)) == true);
    }
}
