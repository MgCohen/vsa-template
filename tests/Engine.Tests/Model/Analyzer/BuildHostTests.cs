namespace CheckModel.Tests;

using UmpireEngine;

public sealed class BuildHostTests {
    [TestCase("csc", true)]
    [TestCase("VBCSCompiler", true)]
    [TestCase("devenv", false)]
    [TestCase("ServiceHub.RoslynCodeAnalysisService", false)]
    [TestCase("dotnet-format", false)]
    [TestCase(null, false)]
    public void Only_a_compiler_process_saves(string? process, bool saves) =>
        Assert.That(BuildHost.IsCompiler(process), Is.EqualTo(saves));
}
