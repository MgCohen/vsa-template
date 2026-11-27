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
        Assert.That(EvaluatedFrameworks().GetValueOrDefault(project), Is.EqualTo("netstandard2.0"),
            "A portable project has one build, netstandard2.0, so an app, the engine and the compiler see one shape of each type. "
            + "If the project is missing, add it to the PortableProject items of Framework.Tests.csproj.");

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

    static Dictionary<string, string> EvaluatedFrameworks() {
        var file = Path.Combine(AppContext.BaseDirectory, "portable-frameworks.txt");
        Assert.That(file, Does.Exist, "Build Framework.Tests: its build writes the target frameworks of the portable projects.");

        return File.ReadAllLines(file)
            .Select(line => line.Split('=', 2))
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => string.Join(";", group.Select(parts => parts[1])), StringComparer.Ordinal);
    }

    static bool IsNetstandard20(string project) {
        var output = Path.Combine(BuiltAssembly.BinFolder, project);
        var dll = Path.Combine(output, BuiltAssembly.Configuration, $"{project}.dll");

        return Directory.Exists(Path.Combine(output, BuiltAssembly.Configuration + "_netstandard2.0"))
            || (File.Exists(dll) && BuiltAssembly.OfSingleTarget(project).TargetFramework == Netstandard20);
    }
}
