namespace Framework.Tests.Unity;

public sealed class PortableTargetTests {
    const string Netstandard20 = ".NETStandard,Version=v2.0";

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Names))]
    public void A_portable_project_has_a_netstandard2_0_library(string project) {
        var assembly = BuiltAssembly.OfPortable(project);

        Assert.That(assembly.TargetFramework, Is.EqualTo(Netstandard20));
    }

    [TestCaseSource(typeof(PortableProjects), nameof(PortableProjects.Names))]
    public void A_portable_project_has_no_other_library(string project) =>
        Assert.That(Directory.GetDirectories(ProjectOutput(project), BuiltAssembly.Configuration + "_*"), Is.Empty,
            "A portable project has one build, netstandard2.0, so an app, the engine and the compiler see one shape of each type. "
            + "If the folder is from an old build, delete artifacts~.");

    [Test]
    public void Each_project_with_a_netstandard2_0_library_is_in_the_portable_list() {
        var netstandardProjects = Directory.GetDirectories(BuiltAssembly.BinFolder)
            .Select(Path.GetFileName)
            .Where(project => IsNetstandard20(project!))
            .Except(PortableProjects.Analyzers)
            .Except(PortableProjects.OtherNetstandard20Projects);

        Assert.That(netstandardProjects, Is.SubsetOf(PortableProjects.Names),
            "Add the project to PortableProjects and to docs/unity.md, or remove it from UmpirePortableLayer. "
            + "If the project is not a portable layer, add it to PortableProjects.OtherNetstandard20Projects.");
    }

    [Test]
    public void The_markers_target_netstandard2_0() {
        Assert.That(BuiltAssembly.OfSingleTarget("Umpire.Markers").TargetFramework, Is.EqualTo(Netstandard20));
    }

    static string ProjectOutput(string project) {
        var output = Path.Combine(BuiltAssembly.BinFolder, project);
        Assert.That(Directory.Exists(output), Is.True, $"{project} has no build output. Build the project first.");

        return output;
    }

    static bool IsNetstandard20(string project) {
        var output = Path.Combine(BuiltAssembly.BinFolder, project);
        var dll = Path.Combine(output, BuiltAssembly.Configuration, $"{project}.dll");

        return Directory.Exists(Path.Combine(output, BuiltAssembly.Configuration + "_netstandard2.0"))
            || (File.Exists(dll) && BuiltAssembly.OfSingleTarget(project).TargetFramework == Netstandard20);
    }
}
