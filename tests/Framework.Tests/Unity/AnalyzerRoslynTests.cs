namespace Framework.Tests.Unity;

public sealed class AnalyzerRoslynTests {
    static readonly Version UnityRoslyn = new(4, 10, 0, 0);

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
