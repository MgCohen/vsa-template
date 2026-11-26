using Tests.Shared;

namespace Rules.Tests;

[NonParallelizable]
public sealed class OneLineBuildTests {
    Workspace workspace = null!;
    DotnetResult first = null!;
    DotnetResult engine = null!;
    DotnetResult build = null!;

    string Engine => Path.Combine(workspace.Env["NUGET_PACKAGES"], "umpire.sdk.web", LocalFeed.Version, "tools", "net8.0",
        "umpire-engine.dll");

    [OneTimeSetUp]
    public void Build() {
        workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "OneLine"));
        File.Delete(Path.Combine(workspace.Root, "Directory.Build.props"));
        first = workspace.Dotnet("build", "App/Tests/OneLine.Tests.csproj", "-nodeReuse:false");
        engine = RunEngine();
        build = workspace.Dotnet("build", "App/Tests/OneLine.Tests.csproj", "--no-incremental", "-nodeReuse:false");
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
    [TestCase("App/Tests/bin")]
    [TestCase("App/Tests/obj")]
    [TestCase("App/Tests/artifacts~")]
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
    public void A_root_project_with_more_than_100_files_does_not_compile_the_files_of_a_nested_project() {
        var folder = Path.Combine(workspace.Src, "App", "Many");
        Directory.CreateDirectory(folder);
        foreach (var number in Enumerable.Range(1, 110)) {
            File.WriteAllText(Path.Combine(folder, $"Part{number}.cs"),
                $"namespace OneLine.App.Many;\n\ninternal static class Part{number};\n");
        }
        try {
            var files = Evaluate("OneLine.csproj", "-getItem:Compile");
            var run = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheck=false");

            Assert.That(files.Output, Does.Contain("Part110.cs").And.Not.Contain("AppTests.cs"), files.Output);
            Assert.That(run.Succeeded, Is.True, run.Output);
            Assert.That(run.Output, Does.Not.Contain("CS0436"), run.Output);
        } finally {
            Directory.Delete(folder, true);
        }
    }

    [Test]
    public void The_sdk_package_has_the_rule_engine() =>
        Assert.That(File.Exists(Engine), Is.True, first.Output);

    [Test]
    public void The_engine_of_the_sdk_package_targets_net8_with_roll_forward_Major() {
        var config = File.ReadAllText(Path.ChangeExtension(Engine, ".runtimeconfig.json"));
        Assert.That(config, Does.Contain("\"tfm\": \"net8.0\"").And.Contain("\"rollForward\": \"Major\"")
            .And.Not.Contain("Microsoft.AspNetCore.App"), config);
    }

    [Test]
    public void The_package_path_runs_the_engine_on_the_newest_runtime() {
        var value = Evaluate("OneLine.csproj", "-getProperty:UmpireEngineRollForward");
        Assert.That(value.Output.Trim(), Is.EqualTo("LatestMajor"), value.Output);
    }

    [Test]
    public void The_package_path_gives_the_runtime_flag_before_the_engine_file() {
        var run = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false", "-v:n");
        var command = Lines(run, "umpire-engine.dll\" --stage Check").FirstOrDefault() ?? "";

        Assert.That(command, Does.Contain("exec --roll-forward LatestMajor \""), run.Output);
    }

    [Test]
    public void The_source_path_runs_the_engine_on_the_newest_runtime() {
        var value = Dotnet.Run(Repo.Root, null, "msbuild", Repo.Path("tests", "Fixtures", "Notes", "Notes.csproj"),
            "-getProperty:UmpireEngineRollForward", "-nodeReuse:false").EnsureSuccess();
        Assert.That(value.Output.Trim(), Is.EqualTo("LatestMajor"), value.Output);
    }

    [Test]
    public void The_source_path_gives_the_runtime_flag_before_the_engine_file() {
        var preprocessed = Path.Combine(TempFolder.Create(), "Notes.xml");
        try {
            Dotnet.Run(Repo.Root, null, "msbuild", Repo.Path("tests", "Fixtures", "Notes", "Notes.csproj"),
                $"-preprocess:{preprocessed}", "-nodeReuse:false").EnsureSuccess();
            var text = File.ReadAllText(preprocessed);

            Assert.That(text, Does.Contain("exec --roll-forward $(UmpireEngineRollForward) &quot;$(UmpireEngineFile)&quot;"));
        } finally {
            TempFolder.Delete(Path.GetDirectoryName(preprocessed)!);
        }
    }

    [Test]
    public void A_build_runs_the_architecture_check() =>
        Assert.That(Lines(first, "umpire-engine: "), Is.Not.Empty, first.Output);

    [Test]
    public void An_engine_that_crashes_fails_the_build_in_Error_mode() {
        var notAnEngine = Path.Combine(workspace.Src, ".umpireconfig");
        var crashed = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false",
            $"-p:UmpireEngineFile={notAnEngine}", "-p:UmpireCheckMode=Error");

        Assert.That(crashed.Succeeded, Is.False, crashed.Output);
        Assert.That(crashed.Output, Does.Contain("error UMP030004"), crashed.Output);
    }

    [TestCase(1, true)]
    [TestCase(2, false)]
    public void An_engine_that_exits_with_a_warning_line_is_a_crash_only_with_another_code_than_1(int code, bool isRun) {
        const string line = "umpire-engine: warning UMP030401: probe";
        var host = Path.Combine(workspace.Root, OperatingSystem.IsWindows() ? $"warn{code}.cmd" : $"warn{code}.sh");
        File.WriteAllText(host, OperatingSystem.IsWindows()
            ? $"@echo {line}\r\n@exit /b {code}\r\n"
            : $"#!/bin/sh\necho \"{line}\"\nexit {code}\n");
        if (!OperatingSystem.IsWindows()) {
            File.SetUnixFileMode(host, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var run = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false", $"-p:_UmpireDotnet={host}");

        Assert.That(run.Succeeded, Is.True, run.Output);
        Assert.That(run.Output, Does.Contain("warning UMP030401"));
        Assert.That(run.Output.Contains("warning UMP030004", StringComparison.Ordinal), Is.Not.EqualTo(isRun), run.Output);
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
            $"-p:_UmpireDotnet={host}", "-p:UmpireCheckMode=Error");

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
        var result = workspace.Dotnet("test", "--project", "App/Tests/OneLine.Tests.csproj");
        Assert.That(result.Succeeded, Is.True, result.Output);
    }

    [Test]
    public void A_file_pattern_section_gives_the_TOML_error() {
        var file = Path.Combine(workspace.Src, ".umpireconfig");
        var text = File.ReadAllText(file);
        File.WriteAllText(file, text + "\n[*.cs]\numpire.UMP010102.max = 1\n");
        try {
            var run = workspace.Dotnet("build", "OneLine.csproj", "--no-restore", "-nodeReuse:false");

            Assert.That(run.Output, Does.Contain("warning UMP010002"), run.Output);
        } finally {
            File.WriteAllText(file, text);
        }
    }

    [Test]
    public void An_artifacts_path_of_a_Directory_Build_props_file_wins() {
        var props = Path.Combine(workspace.Src, "App", "Tests", "Directory.Build.props");
        File.WriteAllText(props, """
            <Project>
              <PropertyGroup>
                <ArtifactsPath>$(MSBuildThisFileDirectory)out~</ArtifactsPath>
              </PropertyGroup>
            </Project>
            """);
        try {
            var path = Evaluate("App/Tests/OneLine.Tests.csproj", "-getProperty:ArtifactsPath");
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
    [TestCase("App/Tests/OneLine.Tests.csproj")]
    public void A_project_runs_no_rule_engine(string project, params string[] switches) {
        var value = Evaluate([project, "-getProperty:UmpireEngine", .. switches]);
        Assert.That(value.Output.Trim(), Is.EqualTo("false"), value.Output);
    }

    [Test]
    public void The_root_project_gets_no_engine_package() {
        var references = Evaluate("OneLine.csproj", "-getItem:PackageReference");
        Assert.That(references.Output, Does.Not.Contain("Umpire.Engine"), references.Output);
    }

    [Test]
    public void The_web_root_project_gets_the_runtime_and_the_web_bundles() {
        var references = Evaluate("OneLine.csproj", "-getItem:PackageReference");
        Assert.That(references.Output, Does.Contain("\"Umpire\"").And.Contain("\"Umpire.Web\"").And.Not.Contain("\"Umpire.Vsa"),
            references.Output);
    }

    [Test]
    public void The_root_project_gets_no_definition_package() {
        var references = Evaluate("OneLine.csproj", "-getItem:PackageReference");
        Assert.That(references.Output, Does.Not.Contain("\"Umpire.Definition\"").And.Not.Contain("\"Umpire.Vsa.Definition\""),
            references.Output);
    }

    [Test]
    public void The_root_project_gives_the_definition_of_the_pack_only_to_the_check() {
        var bin = Path.Combine(workspace.Src, "artifacts~", "bin", "OneLine", "debug");
        Assert.Multiple(() => {
            foreach (var dll in new[] { "Umpire.Definition.dll", "Umpire.Vsa.Definition.dll" }) {
                Assert.That(File.Exists(Path.Combine(bin, "umpire-definitions", dll)), Is.True, dll);
                Assert.That(File.Exists(Path.Combine(bin, dll)), Is.False, dll);
            }
        });
    }

    [Test]
    public void The_test_project_gets_the_definition_packages_with_the_test_bases() {
        var assets = File.ReadAllText(Path.Combine(workspace.Src, "artifacts~", "obj", "OneLine.Tests", "project.assets.json"));
        Assert.That(assets, Does.Contain("\"Umpire.Definition/").And.Contain("\"Umpire.Vsa.Definition/"));
    }

    [Test]
    public void The_test_bases_of_the_default_pack_can_be_left_out() {
        var references = Evaluate("App/Tests/OneLine.Tests.csproj", "-getItem:PackageReference", "-p:UmpireDefaultPack=false");
        Assert.That(references.Output, Does.Not.Contain("\"Umpire.Testing\""), references.Output);
        Assert.That(references.Output, Does.Contain("\"Umpire.Definition\""), references.Output);
    }

    DotnetResult RunEngine() => workspace.Dotnet("exec", Engine, "--project", workspace.Src,
        "--bin", Path.Combine(workspace.Src, "artifacts~", "bin", "OneLine", "debug"));

    DotnetResult Evaluate(params string[] args) =>
        workspace.Dotnet(["msbuild", .. args, "-nodeReuse:false"]).EnsureSuccess();

    static IEnumerable<string> Lines(DotnetResult result, string text) =>
        result.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));
}
