using Tests.Shared;

namespace Rules.Tests;

// The formatting rules in a real build from the packed VSA.Sdk: VSA.Rules.props turns them on, and they are warnings.
// The built fixture tests/Fixtures/Built/Formatting has one file in Infrastructure/ for each rule,
// and a file in Infrastructure/Migrations/ that breaks the rules, but is generated code.
[NonParallelizable]
public sealed class FormattingBuildTests {
    DotnetResult build = null!;

    [OneTimeSetUp]
    public void Build() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Formatting"));
        build = workspace.Dotnet("build", "Modules/Styled/Styled.csproj", "-nodeReuse:false");
    }

    // TreatWarningsAsErrors is true in VSA.Sdk, but the formatting rules are in WarningsNotAsErrors.
    [Test]
    public void Build_passes() => Assert.That(build.Succeeded, Is.True, build.Output);

    [TestCase("VSA1008", "LongLine.cs")]
    [TestCase("VSA1009", "LongMethod.cs")]
    [TestCase("VSA1010", "LongFile.cs")]
    [TestCase("VSA1011", "TwoTypes.cs")]
    [TestCase("VSA1012", "MemberOrder.cs")]
    [TestCase("IDE0161", "BuiltInRules.cs")]
    [TestCase("IDE0055", "BuiltInRules.cs")]
    [TestCase("IDE0040", "BuiltInRules.cs")]
    [TestCase("IDE2000", "BuiltInRules.cs")]
    [TestCase("IDE0007", "BuiltInRules.cs")]
    [TestCase("IDE0011", "BuiltInRules.cs")]
    public void Rule_gives_a_warning(string rule, string file) =>
        Assert.That(Lines($"warning {rule}").Where(line => line.Contains(file, StringComparison.Ordinal)), Is.Not.Empty, build.Output);

    [Test]
    public void Rules_give_no_errors() => Assert.That(Lines(": error "), Is.Empty);

    // VSA.Sdk marks the files in Infrastructure/Migrations/ as generated code, so no rule checks them.
    [Test]
    public void Migrations_give_no_warnings() =>
        Assert.That(Lines(": warning ").Where(line => line.Contains("Migrations", StringComparison.Ordinal)), Is.Empty);

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
