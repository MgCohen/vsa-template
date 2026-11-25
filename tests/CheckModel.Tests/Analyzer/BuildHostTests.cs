namespace CheckModel.Tests;

using System.Reflection;
using Umpire.CheckModel.Analyzer;

public sealed class BuildHostTests {
    [TestCase("csc", true)]
    [TestCase("VBCSCompiler", true)]
    [TestCase("devenv", false)]
    [TestCase("ServiceHub.RoslynCodeAnalysisService", false)]
    [TestCase("dotnet-format", false)]
    [TestCase(null, false)]
    public void Only_a_compiler_process_saves(string? process, bool saves) =>
        Assert.That(BuildHost.IsCompiler(process), Is.EqualTo(saves));

    [Test]
    public void A_compiler_process_that_loaded_the_Workspaces_assembly_saves() {
        Assembly.Load("Microsoft.CodeAnalysis.Workspaces");

        Assert.That(BuildHost.IsCompiler("VBCSCompiler"), Is.True);
    }
}
