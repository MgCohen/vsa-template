namespace Framework.Tests.Unity;

public sealed class AnalyzerRoslynTests {
    static readonly Version UnityRoslyn = new(4, 10, 0, 0);
    static readonly Version UnityRuntime = new(8, 0, 0, 0);

    static readonly Dictionary<string, Version> UnityCompilerAssemblies = new() {
        ["netstandard"] = new(2, 0, 0, 0),
        ["System.Collections.Immutable"] = new(8, 0, 0, 0),
        ["System.Reflection.Metadata"] = new(8, 0, 0, 0),
        ["Microsoft.CodeAnalysis"] = UnityRoslyn,
        ["Microsoft.CodeAnalysis.CSharp"] = UnityRoslyn,
    };

    [TestCaseSource(typeof(BuiltAssembly), nameof(BuiltAssembly.UmpireAnalyzerFiles))]
    public void An_analyzer_file_references_only_the_Unity_compiler_and_the_other_analyzer_files(string file) {
        var shipped = BuiltAssembly.AnalyzerFiles().Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.Ordinal);
        var foreign = BuiltAssembly.OfAnalyzer(file).References
            .Where(reference => !shipped.Contains(reference.Name))
            .Where(reference => !IsOfTheUnityCompiler(reference))
            .Select(reference => $"{reference.Name} {reference.Version}");

        Assert.That(foreign, Is.Empty);
    }

    [Test]
    public void The_analyzer_entry_references_the_Roslyn_version_of_Unity_at_most() {
        var roslyn = BuiltAssembly.OfAnalyzer(BuiltAssembly.AnalyzerEntry).References
            .Where(reference => reference.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .ToList();

        Assert.That(roslyn, Is.Not.Empty);
        Assert.That(roslyn.Select(reference => reference.Version), Has.All.LessThanOrEqualTo(UnityRoslyn));
    }

    [TestCaseSource(typeof(BuiltAssembly), nameof(BuiltAssembly.UmpireAnalyzerFiles))]
    public void An_analyzer_file_targets_netstandard2_0(string file) {
        Assert.That(BuiltAssembly.OfAnalyzer(file).TargetFramework, Is.EqualTo(".NETStandard,Version=v2.0"));
    }

    static bool IsOfTheUnityCompiler(AssemblyReference reference) =>
        UnityCompilerAssemblies.TryGetValue(reference.Name, out var owned)
            ? reference.Version <= owned
            : reference.Name.StartsWith("System.", StringComparison.Ordinal) && reference.Version <= UnityRuntime;
}
