using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class OneLineBuildTests {
    Workspace workspace = null!;
    DotnetResult first = null!;
    DotnetResult engine = null!;
    DotnetResult build = null!;

    string Engine => Path.Combine(workspace.Env["NUGET_PACKAGES"], "umpire.sdk.web", LocalFeed.Version, "tools", "net10.0",
        "umpire-engine.dll");

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
    public void The_engine_run_builds_the_app_and_checks_it() =>
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
    public void A_value_in_the_umpireconfig_file_reaches_the_analyzers_in_the_first_build() =>
        Assert.That(Lines(first, "warning UMP010101").Where(line => line.Contains("OneLineApp.cs", StringComparison.Ordinal)),
            Is.Not.Empty, first.Output);

    [Test]
    public void The_compiler_gets_the_umpireconfig_file_as_an_additional_file() {
        var files = Evaluate("OneLine.csproj", "-getItem:AdditionalFiles");
        Assert.That(files.Output, Does.Contain(".umpireconfig"), files.Output);
    }

    [Test]
    public void The_sdk_package_has_the_rule_engine() =>
        Assert.That(File.Exists(Engine), Is.True, first.Output);

    [Test]
    public void A_build_runs_the_architecture_check() =>
        Assert.That(Lines(first, "umpire-engine: "), Is.Not.Empty, first.Output);

    [Test]
    public void An_engine_that_crashes_fails_the_build() {
        var notAnEngine = Path.Combine(workspace.Src, ".umpireconfig");
        var crashed = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false",
            $"-p:UmpireEngineFile={notAnEngine}");

        Assert.That(crashed.Succeeded, Is.False, crashed.Output);
        Assert.That(crashed.Output, Does.Contain("error UMP030004"), crashed.Output);
    }

    [Test]
    public void A_crash_line_with_the_word_error_is_not_a_finding() {
        const string crash = "System.IO.IOException: error reading: the file 'a,b' is locked";
        var host = Path.Combine(workspace.Root, OperatingSystem.IsWindows() ? "crash.cmd" : "crash.sh");
        File.WriteAllText(host, OperatingSystem.IsWindows()
            ? $"@echo {crash}\r\n@exit /b 1\r\n"
            : $"#!/bin/sh\necho \"{crash}\"\nexit 1\n");
        if (!OperatingSystem.IsWindows()) {
            File.SetUnixFileMode(host, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var crashed = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false",
            $"-p:_UmpireDotnet={host}");

        Assert.That(crashed.Succeeded, Is.False, crashed.Output);
        Assert.That(crashed.Output, Does.Contain("error UMP030004"), crashed.Output);
    }

    [Test]
    public void A_build_with_UmpireCheck_false_does_not_run_the_check() {
        var quiet = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheck=false");
        Assert.That(Lines(quiet, "umpire-engine: "), Is.Empty, quiet.Output);
    }

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
    public void A_file_pattern_section_stops_the_build_with_the_TOML_error() {
        var file = Path.Combine(workspace.Src, ".umpireconfig");
        var text = File.ReadAllText(file);
        File.WriteAllText(file, text + "\n[*.cs]\numpire.UMP010102.max = 1\n");
        try {
            var run = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false");

            Assert.That(run.Succeeded, Is.False, run.Output);
            Assert.That(run.Output, Does.Contain("error UMP010002"), run.Output);
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

    [Test]
    public void An_engine_file_of_a_Directory_Build_props_file_wins() {
        var props = Path.Combine(workspace.Src, "Directory.Build.props");
        File.WriteAllText(props, """
            <Project>
              <PropertyGroup>
                <UmpireEngineFile>C:/tools/my-engine.dll</UmpireEngineFile>
              </PropertyGroup>
            </Project>
            """);
        try {
            var path = Evaluate("OneLine.csproj", "-getProperty:UmpireEngineFile");
            Assert.That(path.Output.Trim(), Is.EqualTo("C:/tools/my-engine.dll"), path.Output);
        } finally {
            File.Delete(props);
        }
    }

    [TestCase("OneLine.csproj", "-p:UmpireEngine=false")]
    [TestCase("Tests/OneLine.Tests.csproj")]
    public void A_project_runs_no_rule_engine(string project, params string[] switches) {
        var value = Evaluate([project, "-getProperty:UmpireEngine", .. switches]);
        Assert.That(value.Output.Trim(), Is.EqualTo("false"), value.Output);
    }

    [Test]
    public void The_root_project_gets_no_engine_package() {
        var references = Evaluate("OneLine.csproj", "-getItem:PackageReference");
        Assert.That(references.Output, Does.Not.Contain("Umpire.Engine"), references.Output);
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

    static IEnumerable<string> Lines(DotnetResult result, string text) =>
        result.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
