namespace Framework.Tests.Unity;

public sealed class PortableTargetTests {
    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Names))]
    public void A_portable_project_has_a_netstandard2_1_library(string project) {
        var assembly = BuiltAssembly.OfMultiTarget(project, "netstandard2.1");

        Assert.That(assembly.TargetFramework, Is.EqualTo(".NETStandard,Version=v2.1"));
    }

    [Test]
    public void Each_project_with_a_netstandard2_1_library_is_in_the_portable_list() {
        var netstandardProjects = Directory.GetDirectories(BuiltAssembly.BinFolder)
            .Where(project => Directory.Exists(Path.Combine(project, $"{BuiltAssembly.Configuration}_netstandard2.1")))
            .Select(Path.GetFileName);

        Assert.That(netstandardProjects, Is.SubsetOf(PortableProjects.Names), "Add the project to PortableProjects and to docs/unity.md, or remove it from UmpirePortableLayer.");
    }

    [Test]
    public void The_markers_target_netstandard2_0() {
        Assert.That(BuiltAssembly.OfSingleTarget("Umpire.Markers").TargetFramework, Is.EqualTo(".NETStandard,Version=v2.0"));
    }
}
