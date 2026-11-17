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
    public void The_compiler_gets_the_top_level_keys_of_the_umpireconfig_file() {
        var obj = Path.Combine(workspace.Src, "artifacts~", "obj", "OneLine");
        var copy = Directory.EnumerateFiles(obj, "Umpire.Block.globalconfig", SearchOption.AllDirectories).Single();
        var source = File.ReadAllLines(Path.Combine(workspace.Src, ".umpireconfig"))
            .TakeWhile(line => !line.TrimStart().StartsWith('['));
        var header = new[] { "is_global = true", "global_level = 101" };
        Assert.That(NotBlank(File.ReadAllLines(copy)), Is.EqualTo(NotBlank(header.Concat(source))));
    }

    [Test]
    public void A_section_in_the_umpireconfig_file_gives_a_warning_and_no_value() {
        Assert.That(Lines("warning UMP010002"), Is.Not.Empty, build.Output);
        Assert.That(Lines("warning UMP010102"), Is.Empty, build.Output);
    }

    [Test]
    public void An_artifacts_path_of_a_Directory_Build_props_file_wins() {
        var props = Path.Combine(workspace.Src, "Tests", "Directory.Build.props");
        File.WriteAllText(props, """
            <Project>
              <PropertyGroup>
                <ArtifactsPath>$(MSBuildThisFileDirectory)out~</ArtifactsPath>
              </PropertyGroup>
            </Project>
            """);
        try {
            var path = Evaluate("Tests/OneLine.Tests.csproj", "-getProperty:ArtifactsPath");
            Assert.That(path.Output.Trim(), Does.EndWith("out~"), path.Output);
        } finally {
            File.Delete(props);
        }
    }

    [TestCase("OneLine.csproj", "-p:UmpireEngine=false")]
    [TestCase("Tests/OneLine.Tests.csproj", "-p:UmpireEngine=")]
    public void A_project_gets_no_rule_engine(string project, string engine) {
        var references = Evaluate(project, "-getItem:PackageReference", engine);
        Assert.That(references.Output, Does.Not.Contain("Umpire.Engine.Build"), references.Output);
    }

    static IEnumerable<string> NotBlank(IEnumerable<string> lines) => lines.Where(line => line.Trim().Length > 0);

    [TestCase("OneLine.csproj", "Umpire.Vsa")]
    [TestCase("Tests/OneLine.Tests.csproj", "Umpire.Vsa.Testing.Unit")]
    public void The_default_pack_can_be_left_out(string project, string package) {
        var references = Evaluate(project, "-getItem:PackageReference", "-p:UmpireDefaultPack=false");
        Assert.That(references.Output, Does.Not.Contain($"\"{package}\""), references.Output);
    }

    DotnetResult Evaluate(params string[] args) =>
        workspace.Dotnet(["msbuild", .. args, "-nodeReuse:false"]).EnsureSuccess();

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
