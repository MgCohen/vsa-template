namespace Framework.Tests.Unity;

public sealed class AnalyzerRoslynTests {
    static readonly Version UnityRoslyn = new(4, 10, 0, 0);

    static readonly Dictionary<string, Version> UnityCompilerAssemblies = new() {
        ["netstandard"] = new(2, 0, 0, 0),
        ["System.Collections.Immutable"] = new(8, 0, 0, 0),
        ["System.Reflection.Metadata"] = new(8, 0, 0, 0),
        ["Microsoft.CodeAnalysis"] = UnityRoslyn,
        ["Microsoft.CodeAnalysis.CSharp"] = UnityRoslyn,
    };

    static readonly string[] UmpireAnalyzerAssemblies = ["Umpire.Kernels.Analyzers"];

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Analyzers))]
    public void An_analyzer_references_only_the_assemblies_of_the_Unity_compiler(string analyzer) {
        var foreign = BuiltAssembly.OfSingleTarget(analyzer).References
            .Where(reference => !UmpireAnalyzerAssemblies.Contains(reference.Name))
            .Where(reference => !UnityCompilerAssemblies.TryGetValue(reference.Name, out var owned) || reference.Version > owned)
            .Select(reference => $"{reference.Name} {reference.Version}");

        Assert.That(foreign, Is.Empty);
    }

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Analyzers))]
    public void An_analyzer_references_the_Roslyn_version_of_Unity_at_most(string analyzer) {
        var roslyn = BuiltAssembly.OfSingleTarget(analyzer).References
            .Where(reference => reference.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .ToList();

        Assert.That(roslyn, Is.Not.Empty);
        Assert.That(roslyn.Select(reference => reference.Version), Has.All.LessThanOrEqualTo(UnityRoslyn));
    }

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Analyzers))]
    public void An_analyzer_targets_netstandard2_0(string analyzer) {
        Assert.That(BuiltAssembly.OfSingleTarget(analyzer).TargetFramework, Is.EqualTo(".NETStandard,Version=v2.0"));
    }
}
