using System.Text.Json;
using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class DesignTimeBuildTests {
    DotnetResult build = null!;
    JsonElement result;

    [OneTimeSetUp]
    public void Build() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Formatting"));
        build = workspace.Dotnet("msbuild", "Features/Styled/Styled.csproj", "-restore", "-nodeReuse:false",
            "-t:Compile", "-p:DesignTimeBuild=true", "-p:SkipCompilerExecution=true", "-p:ProvideCommandLineArgs=true",
            "-getItem:CscCommandLineArgs", "-getProperty:RunAnalyzersDuringLiveAnalysis", "-getProperty:RunAnalyzers");
        Assert.That(build.Succeeded, Is.True, build.Output);
        var json = build.Output[build.Output.IndexOf('{')..(build.Output.LastIndexOf('}') + 1)];
        result = JsonDocument.Parse(json).RootElement.Clone();
    }

    [Test]
    public void The_design_time_build_passes() => Assert.That(build.Succeeded, Is.True, build.Output);

    [Test]
    public void The_editor_reads_the_severities_of_Umpire_globalconfig() =>
        Assert.That(Arguments("/analyzerconfig:"), Has.Some.EndsWith("Umpire.globalconfig"), build.Output);

    [TestCase("Umpire.Kernel.Analyzers.globalconfig")]
    [TestCase("Umpire.Vsa.Analyzers.globalconfig")]
    public void The_editor_reads_the_rule_values_of_each_analyzer_package(string file) =>
        Assert.That(Arguments("/analyzerconfig:").Select(Path.GetFileName), Has.Member(file), build.Output);

    [TestCase("Microsoft.CodeAnalysis.CSharp.CodeStyle.dll")]
    [TestCase("Umpire.Kernel.Analyzers.dll")]
    [TestCase("Umpire.Vsa.Analyzers.dll")]
    public void The_editor_runs_the_analyzer(string file) =>
        Assert.That(Arguments("/analyzer:").Select(Path.GetFileName), Has.Member(file), build.Output);

    [TestCase("RunAnalyzersDuringLiveAnalysis")]
    [TestCase("RunAnalyzers")]
    public void The_live_analysis_is_not_off(string property) =>
        Assert.That(result.GetProperty("Properties").GetProperty(property).GetString(),
            Is.Not.EqualTo("false").IgnoreCase, build.Output);

    IEnumerable<string> Arguments(string prefix) =>
        result.GetProperty("Items").GetProperty("CscCommandLineArgs").EnumerateArray()
            .Select(item => item.GetProperty("Identity").GetString()!)
            .Where(argument => argument.StartsWith(prefix, StringComparison.Ordinal))
            .Select(argument => argument[prefix.Length..].Trim('"'));
}
