using Build.Infrastructure;
using Steps.Domain.Shared;

namespace Engine.Tests;

public sealed class BuildFindingsTests {
    [Test]
    public void A_located_line_gives_the_file_the_line_the_column_and_the_code() {
        var line = @"C:\app\Foo.cs(4,90): warning UMP010106: Remove the comment. [C:\app\App.csproj]";

        var findings = BuildFindings.Of([line]);

        Assert.That(findings, Is.EqualTo(new[] {
            new Finding("UMP010106", FindingSeverity.Warning, "Remove the comment.", @"C:\app\Foo.cs", 4, 90),
        }));
    }

    [Test]
    public void A_line_with_an_end_position_keeps_the_start() {
        var findings = BuildFindings.Of([@"C:\app\Bar.cs(2,1,2,9): error CS0103: The name 'x' does not exist."]);

        Assert.That(findings.Single(), Is.EqualTo(
            new Finding("CS0103", FindingSeverity.Error, "The name 'x' does not exist.", @"C:\app\Bar.cs", 2, 1)));
    }

    [Test]
    public void A_line_with_no_file_gives_a_finding_with_no_location() {
        var findings = BuildFindings.Of([@"CSC : error AD0001: Analyzer 'A' threw an exception. [C:\app\App.csproj]"]);

        Assert.That(findings.Single(),
            Is.EqualTo(new Finding("AD0001", FindingSeverity.Error, "Analyzer 'A' threw an exception.")));
    }

    [Test]
    public void The_same_diagnostic_from_two_projects_or_the_summary_gives_one_finding() {
        string[] lines = [
            @"C:\app\Foo.cs(1,1): warning CA1812: Text. [C:\app\App.csproj::TargetFramework=net10.0]",
            @"C:\app\Foo.cs(1,1): warning CA1812: Text. [C:\app\App.csproj::TargetFramework=netstandard2.1]",
        ];

        Assert.That(BuildFindings.Of(lines), Has.Count.EqualTo(1));
    }

    [TestCase("Build succeeded.")]
    [TestCase("    3 Warning(s)")]
    [TestCase(@"  App -> C:\app\bin\App.dll")]
    public void A_line_that_is_not_a_diagnostic_gives_no_finding(string line) =>
        Assert.That(BuildFindings.Of([line]), Is.Empty);
}
