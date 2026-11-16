namespace Umpire.Cli.Scaffolding.Setup.Steps;

sealed class BuildPackageStep : SetupStep {
    const string Package = "Umpire.Engine.Build";
    const string CentralFile = "Directory.Packages.props";
    const string CentralSwitch = "ManagePackageVersionsCentrally";

    public override string Id => "build-package";

    public override SetupResult Plan(SetupContext context) {
        var project = context.RootProject;
        var version = context.Version;
        if (context.Read(project) is not string text || XmlText.Parse(text) is not { } document) {
            return ByHand($"{project} is not valid XML. Add the package {Package} {version} to it.");
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
        var reference = $"  <ItemGroup>\n    <PackageReference Include=\"{Package}\" Version=\"{version}\" "
            + "PrivateAssets=\"all\" />\n  </ItemGroup>\n";
        return Change(project, XmlText.AddAtEnd(text, "Project", reference));
    }

    static bool UsesCentralVersions(SetupContext context, string project) {
        var central = context.Read(CentralFile)
            ?? (context.ParentFile(CentralFile) is string parent ? File.ReadAllText(parent) : null);
        return new[] { project, central }.OfType<string>().Any(text =>
            XmlText.Parse(text)?.Descendants().Any(element =>
                element.Name.LocalName == CentralSwitch
                && string.Equals(element.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase)) == true);
    }
}
