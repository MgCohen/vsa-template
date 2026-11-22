namespace Framework.Tests.Unity;

public sealed class PortableTargetTests {
    const string Netstandard21 = ".NETStandard,Version=v2.1";

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Names))]
    public void A_portable_project_has_a_netstandard2_1_library(string project) {
        var assembly = BuiltAssembly.OfPortable(project);

        Assert.That(assembly.TargetFramework, Is.EqualTo(Netstandard21));
    }

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Names))]
    public void A_portable_project_has_no_net8_0_library(string project) =>
        Assert.That(Directory.Exists(Output(project, "_net8.0")), Is.False,
            "A portable project has one build, netstandard2.1, so an app and the engine see one shape of each type.");

    [Test]
    public void Each_project_with_a_netstandard2_1_library_is_in_the_portable_list() {
        var netstandardProjects = Directory.GetDirectories(BuiltAssembly.BinFolder)
            .Select(Path.GetFileName)
            .Where(project => IsNetstandard21(project!));

        Assert.That(netstandardProjects, Is.SubsetOf(PortableProjects.Names),
            "Add the project to PortableProjects and to docs/unity.md, or remove it from UmpirePortableLayer.");
    }

    [Test]
    public void The_markers_target_netstandard2_0() {
        Assert.That(BuiltAssembly.OfSingleTarget("Umpire.Markers").TargetFramework, Is.EqualTo(".NETStandard,Version=v2.0"));
    }

    static string Output(string project, string suffix) =>
        Path.Combine(BuiltAssembly.BinFolder, project, BuiltAssembly.Configuration + suffix);

    static bool IsNetstandard21(string project) =>
        Directory.Exists(Output(project, "_netstandard2.1"))
        || (File.Exists(Path.Combine(Output(project, ""), $"{project}.dll"))
            && BuiltAssembly.OfSingleTarget(project).TargetFramework == Netstandard21);
}
