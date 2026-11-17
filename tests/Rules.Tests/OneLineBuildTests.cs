using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class OneLineBuildTests {
    Workspace workspace = null!;
    DotnetResult build = null!;

    [OneTimeSetUp]
    public void Build() {
        workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "OneLine"));
        File.Delete(Path.Combine(workspace.Root, "Directory.Build.props"));
        build = workspace.Dotnet("build", "Tests/OneLine.Tests.csproj", "-nodeReuse:false");
    }

    [OneTimeTearDown]
    public void Delete() => workspace.Dispose();

    [Test]
    public void The_one_line_projects_build() => Assert.That(build.Succeeded, Is.True, build.Output);

    [TestCase("bin/OneLine")]
    [TestCase("bin/OneLine.Tests")]
    [TestCase("obj/OneLine")]
    [TestCase("obj/OneLine.Tests")]
    public void Each_project_writes_to_the_artifacts_folder_of_the_block(string folder) =>
        Assert.That(Directory.Exists(Path.Combine(workspace.Src, "artifacts~", folder)), Is.True, build.Output);

    [TestCase("bin")]
    [TestCase("obj")]
    [TestCase("Tests/bin")]
    [TestCase("Tests/obj")]
    [TestCase("Tests/artifacts~")]
    public void No_output_folder_is_next_to_the_code(string folder) =>
        Assert.That(Directory.Exists(Path.Combine(workspace.Src, folder)), Is.False);

    [Test]
    public void A_value_in_the_umpireconfig_file_wins_over_the_package_value() =>
        Assert.That(Lines("warning UMP010101").Where(line => line.Contains("OneLineApp.cs", StringComparison.Ordinal)),
            Is.Not.Empty, build.Output);

    [Test]
    public void The_root_project_runs_the_rule_engine() =>
        Assert.That(Directory.EnumerateFiles(Path.Combine(workspace.Src, "artifacts~", "obj", "OneLine"),
            "umpire-check.*", SearchOption.AllDirectories), Is.Not.Empty, build.Output);

    [Test]
    public void The_test_project_runs_its_tests() {
        var result = workspace.Dotnet("test", "--project", "Tests/OneLine.Tests.csproj");
        Assert.That(result.Succeeded, Is.True, result.Output);
    }

    [Test]
    public void The_compiler_gets_the_umpireconfig_file_as_a_global_config() {
        var obj = Path.Combine(workspace.Src, "artifacts~", "obj", "OneLine");
        var copy = Directory.EnumerateFiles(obj, "Umpire.Block.globalconfig", SearchOption.AllDirectories).Single();
        var source = File.ReadAllLines(Path.Combine(workspace.Src, ".umpireconfig"));
        var header = new[] { "is_global = true", "global_level = 100" };
        Assert.That(File.ReadAllLines(copy), Is.EqualTo(header.Concat(source)));
    }

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
