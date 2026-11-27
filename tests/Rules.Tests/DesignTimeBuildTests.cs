using System.Text.Json;
using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
[Category("Package")]
public sealed class DesignTimeBuildTests {
    DotnetResult build = null!;
    JsonElement result;

    [OneTimeSetUp]
    public void Build() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Formatting"));
        build = workspace.Dotnet("msbuild", "App/Features/Styled/Styled.csproj", "-restore", "-nodeReuse:false",
            "-t:Compile", "-p:DesignTimeBuild=true", "-p:SkipCompilerExecution=true", "-p:ProvideCommandLineArgs=true",
            "-getItem:CscCommandLineArgs", "-getProperty:RunAnalyzersDuringLiveAnalysis", "-getProperty:RunAnalyzers");
        Assert.That(build.Succeeded, Is.True, build.Output);
        var json = build.Output[build.Output.IndexOf('{')..(build.Output.LastIndexOf('}') + 1)];
        result = JsonDocument.Parse(json).RootElement.Clone();
    }

    [Test]
    public void The_design_time_build_passes() => Assert.That(build.Succeeded, Is.True, build.Output);

    [Test]
    public void Umpire_Sdk_gives_the_editor_no_style_file() =>
        Assert.That(Arguments("/analyzerconfig:").Select(Path.GetFileName), Has.No.Member("Umpire.globalconfig").And.No.Member("Umpire.Tests.globalconfig"), build.Output);

    [TestCase("Umpire.Engine.Analyzer.dll")]
    [TestCase("Umpire.Engine.dll")]
    [TestCase("Umpire.dll")]
    public void The_editor_runs_the_analyzer(string file) =>
        Assert.That(Arguments("/analyzer:").Select(Path.GetFileName), Has.Member(file), build.Output);

    [TestCase("Microsoft.CodeAnalysis.dll")]
    [TestCase("System.Collections.Immutable.dll")]
    public void The_editor_gets_no_file_of_the_compiler_as_an_analyzer(string file) =>
        Assert.That(Arguments("/analyzer:").Select(Path.GetFileName), Has.No.Member(file), build.Output);

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
