using System.Xml.Linq;
using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
[Category("Package")]
public sealed class FormattingBuildTests {
    static readonly string[] styleFiles = ["Directory.Build.props", "tools/bundle/SourceMode/Umpire.SourceMode.props"];
    static readonly string[] styleSwitches =
        ["AnalysisLevel", "EnforceCodeStyleInBuild", "GenerateDocumentationFile", "NoWarn"];
    Workspace workspace = null!;
    DotnetResult build = null!;

    [OneTimeSetUp]
    public void Build() {
        workspace = StyledWorkspace();
        build = workspace.Dotnet(["build", "App/Features/Styled/Styled.csproj", "-nodeReuse:false", .. RepositoryStyle()]);
    }

    [OneTimeTearDown]
    public void Delete() => workspace?.Dispose();

    [Test]
    public void Build_passes() => Assert.That(build.Succeeded, Is.True, build.Output);

    [TestCase("UMP010101", "LongLine.cs")]
    [TestCase("UMP010102", "LongMethod.cs")]
    [TestCase("UMP010103", "LongFile.cs")]
    [TestCase("UMP010104", "TwoTypes.cs")]
    [TestCase("UMP010105", "MemberOrder.cs")]
    [TestCase("IDE0055", "BuiltInRules.cs")]
    [TestCase("IDE2000", "BuiltInRules.cs")]
    [TestCase("IDE0011", "BuiltInRules.cs")]
    [TestCase("IDE0005", "QualityRules.cs")]
    [TestCase("IDE0051", "QualityRules.cs")]
    [TestCase("IDE0052", "QualityRules.cs")]
    [TestCase("IDE0059", "QualityRules.cs")]
    [TestCase("IDE0060", "QualityRules.cs")]
    [TestCase("IDE0004", "QualityRules.cs")]
    [TestCase("CA2000", "QualityRules.cs")]
    [TestCase("CA1849", "QualityRules.cs")]
    [TestCase("CA1052", "QualityRules.cs")]
    [TestCase("CA1012", "QualityRules.cs")]
    [TestCase("CA1812", "QualityRules.cs")]
    [TestCase("IDE0036", "StyleRules.cs")]
    [TestCase("IDE0054", "StyleRules.cs")]
    [TestCase("IDE1005", "StyleRules.cs")]
    [TestCase("IDE0083", "StyleRules.cs")]
    [TestCase("IDE2002", "StyleRules.cs")]
    [TestCase("IDE1006", "StyleRules.cs")]
    [TestCase("IDE0021", "StyleRules.cs")]
    [TestCase("IDE0023", "StyleRules.cs")]
    [TestCase("IDE0024", "StyleRules.cs")]
    [TestCase("IDE0025", "StyleRules.cs")]
    [TestCase("IDE0026", "StyleRules.cs")]
    [TestCase("IDE0027", "StyleRules.cs")]
    [TestCase("IDE0061", "StyleRules.cs")]
    public void Rule_gives_a_warning(string rule, string file) =>
        Assert.That(Lines($"warning {rule}").Where(line => line.Contains(file, StringComparison.Ordinal)), Is.Not.Empty, build.Output);

    [TestCase("IDE0040")]
    [TestCase("IDE0008")]
    [TestCase("IDE0022")]
    public void Rule_relaxed_until_0_1_0_gives_no_warning(string rule) =>
        Assert.That(Lines($"warning {rule}"), Is.Empty, build.Output);

    [TestCase("IDE0051")]
    [TestCase("IDE0052")]
    [TestCase("IDE0060")]
    [TestCase("CA1812")]
    public void Unused_code_gives_the_warning(string rule) =>
        Assert.That(Lines($"warning {rule}").Where(line => line.Contains("UnusedCode.cs", StringComparison.Ordinal)), Is.Not.Empty, build.Output);

    [Test]
    public void Rules_give_no_errors() => Assert.That(Lines(": error "), Is.Empty);

    [Test]
    public void Migrations_give_no_warnings() =>
        Assert.That(Lines(": warning ").Where(line => line.Contains("Migrations", StringComparison.Ordinal)), Is.Empty);

    [Test]
    public void A_using_outside_the_namespace_gives_no_warning() =>
        Assert.That(Lines("IDE0065"), Is.Empty, build.Output);

    [Test]
    public void Both_namespace_forms_give_no_warning() =>
        Assert.That(Lines("IDE0161"), Is.Empty, build.Output);

    [Test]
    public void Test_project_may_have_underscores_in_names()
    {
        var result = workspace.Dotnet(["build", "Styled.Tests/Styled.Tests.csproj", "-nodeReuse:false", .. RepositoryStyle()]);
        Assert.That(result.Succeeded, Is.True, result.Output);
        Assert.That(result.Output, Does.Not.Contain("CA1707"), result.Output);
    }

    [Test]
    public void The_Umpire_packages_give_a_consumer_no_style() {
        using var plain = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Formatting"));
        var result = plain.Dotnet("build", "App/Features/Styled/Styled.csproj", "-nodeReuse:false");
        var style = result.Output.Split('\n').Where(line => line.Contains(" warning IDE", StringComparison.Ordinal));
        Assert.That(style, Is.Empty, result.Output);
    }

    static Workspace StyledWorkspace() {
        var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Formatting"));
        File.Copy(Repo.Path(".editorconfig"), Path.Combine(workspace.Src, ".editorconfig"));
        return workspace;
    }

    static IEnumerable<string> RepositoryStyle() {
        var settings = styleFiles.SelectMany(file => XDocument.Load(Repo.Path(file)).Descendants())
            .Where(element => styleSwitches.Contains(element.Name.LocalName))
            .Select(element => Switch(element.Name.LocalName, element.Value));
        return [.. settings, "-p:TreatWarningsAsErrors=false"];
    }

    static string Switch(string name, string value) =>
        $"-p:{name}={value.Replace($"$({name});", "", StringComparison.Ordinal)}";

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
