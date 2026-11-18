using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class OneLineBuildTests {
    Workspace workspace = null!;
    DotnetResult first = null!;
    DotnetResult engine = null!;
    DotnetResult build = null!;

    string Engine => Path.Combine(workspace.Env["NUGET_PACKAGES"], "umpire.engine.build", LocalFeed.Version, "tools", "net10.0",
        "umpire-engine.dll");

    string Copy => Path.Combine(workspace.Src, "artifacts~", "Umpire.Block.globalconfig");

    [OneTimeSetUp]
    public void Build() {
        workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "OneLine"));
        File.Delete(Path.Combine(workspace.Root, "Directory.Build.props"));
        first = workspace.Dotnet("build", "Tests/OneLine.Tests.csproj", "-nodeReuse:false");
        engine = RunEngine();
        build = workspace.Dotnet("build", "Tests/OneLine.Tests.csproj", "--no-incremental", "-nodeReuse:false");
    }

    [OneTimeTearDown]
    public void Delete() => workspace.Dispose();

    [Test]
    public void The_one_line_projects_build() {
        Assert.That(first.Succeeded, Is.True, first.Output);
        Assert.That(build.Succeeded, Is.True, build.Output);
    }

    [Test]
    public void The_engine_run_writes_the_rule_values_builds_the_app_and_checks_it() =>
        Assert.That(engine.Succeeded, Is.True, engine.Output);

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
    public void The_root_project_gets_the_rule_engine_package() =>
        Assert.That(File.Exists(Engine), Is.True, first.Output);

    [Test]
    public void A_build_does_not_run_the_rule_engine() =>
        Assert.That(Lines("UMP03"), Is.Empty, build.Output);

    [Test]
    public void A_build_with_no_engine_run_uses_the_values_of_the_analyzer_package() =>
        Assert.That(first.Output.Split('\n').Where(line => line.Contains("warning UMP010101", StringComparison.Ordinal)
            && line.Contains("OneLineApp.cs", StringComparison.Ordinal)), Is.Empty, first.Output);

    [Test]
    public void The_umpireconfig_file_is_in_the_output_folder() =>
        Assert.That(File.Exists(Path.Combine(workspace.Src, "artifacts~", "bin", "OneLine", "debug", ".umpireconfig")),
            Is.True, first.Output);

    [Test]
    public void The_test_project_runs_its_tests() {
        var result = workspace.Dotnet("test", "--project", "Tests/OneLine.Tests.csproj");
        Assert.That(result.Succeeded, Is.True, result.Output);
    }

    [Test]
    public void The_engine_writes_the_values_of_the_umpireconfig_file_for_the_compiler() =>
        Assert.That(File.ReadAllLines(Copy),
            Is.EqualTo(new[] { "is_global = true", "global_level = 101", "umpire.UMP010101.max = 60" }), engine.Output);

    [Test]
    public void A_file_pattern_section_stops_the_engine_run_with_the_line_of_the_TOML_error() {
        var file = Path.Combine(workspace.Src, ".umpireconfig");
        var text = File.ReadAllText(file);
        File.WriteAllText(file, text + "\n[*.cs]\numpire.UMP010102.max = 1\n");
        try {
            var run = RunEngine();

            Assert.That(run.Succeeded, Is.False, run.Output);
            Assert.That(run.Output, Does.Contain(".umpireconfig(4,").And.Contain("UMP030001"), run.Output);
        } finally {
            File.WriteAllText(file, text);
        }
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

    [TestCase("OneLine.csproj", "Umpire.Vsa")]
    [TestCase("Tests/OneLine.Tests.csproj", "Umpire.Vsa.Testing.Unit")]
    public void The_default_pack_can_be_left_out(string project, string package) {
        var references = Evaluate(project, "-getItem:PackageReference", "-p:UmpireDefaultPack=false");
        Assert.That(references.Output, Does.Not.Contain($"\"{package}\""), references.Output);
    }

    DotnetResult RunEngine() => workspace.Dotnet("exec", Engine, "--project", workspace.Src,
        "--bin", Path.Combine(workspace.Src, "artifacts~", "bin", "OneLine", "debug"));

    DotnetResult Evaluate(params string[] args) =>
        workspace.Dotnet(["msbuild", .. args, "-nodeReuse:false"]).EnsureSuccess();

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
