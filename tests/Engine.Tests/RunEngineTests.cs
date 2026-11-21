using Blocks;
using Build;
using Changes;
using Changes.Infrastructure;
using LoadApp;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using Engine.Slices.RunEngine;
using Microsoft.Extensions.DependencyInjection;
using Report;
using Report.Domain.Shared;
using Report.Slices.WriteReport;
using Rules;
using Steps;
using Steps.Domain.Shared;
using Versions;
using Tests.Shared;
using Umpire.Engine.Versions;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Kernels;
using UmpireEngine;

namespace Engine.Tests;

public sealed class RunEngineTests {
    const string RulesSkipped =
        "umpire-engine: UMP030002: CheckRules skipped, because an earlier step failed.";
    const string PinnedSdk = "global.json: Umpire.Sdk 0.0.1";
    static readonly string Notes = Repo.Path("tests", "Fixtures", "Notes");

    [Test]
    public void The_scan_finds_each_module_of_the_one_engine_assembly() {
        var engine = typeof(StepsPlugin).Assembly;

        var modules = new AppTypes(null).Modules.Where(module => module.GetType().Assembly == engine)
            .Select(module => module.Name);

        Assert.That(modules, Is.EqualTo(new[] { "BuiltApp", "Steps", "Blocks", "Build", "Changes", "Engine", "LoadApp", "Report", "Rules", "Versions" }));
    }

    [Test]
    public void The_engine_reads_the_architecture_from_the_pack_of_the_app_class() {
        using var reader = new BinReader(AppContext.BaseDirectory);
        var app = reader.Index().Dlls.Single(d => d.Name == "umpire-engine").AppClasses.Single();

        var architecture = new AppLoader(AppContext.BaseDirectory).ArchitectureOfApp(app, typeof(EngineApp).Assembly.Location);

        Assert.That(architecture.GetType().FullName, Is.EqualTo(typeof(EngineArchitecture).FullName));
    }

    [Test]
    public void The_engine_makes_the_pack_when_the_pack_gives_no_architecture_type_argument() {
        var pack = typeof(EngineApp).BaseType!.GetGenericArguments()[0];
        var app = new AppClass(Name(typeof(EngineApp)), "", Name(pack), null, []);

        var architecture = new AppLoader(AppContext.BaseDirectory).ArchitectureOfApp(app, typeof(EngineApp).Assembly.Location);

        Assert.That(architecture.GetType().FullName, Is.EqualTo(typeof(EngineArchitecture).FullName));
    }

    static TypeName Name(Type type) => new(type.FullName!, type.Assembly.GetName().Name!);

    [Test]
    public void A_dll_with_no_app_class_has_no_app_class_in_the_index() {
        using var reader = new BinReader(AppContext.BaseDirectory);

        var shared = reader.Index().Dlls.Single(d => d.Name == typeof(Repo).Assembly.GetName().Name);

        Assert.That(shared.AppClasses, Is.Empty);
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_Notes() {
        var result = await Run();

        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.Contains(": error "));
    }

    [Test]
    public async Task An_Umpire_version_that_is_not_the_engine_version_gives_UMP030401_and_skips_the_load() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "Old.csproj"), """<Project Sdk="Umpire.Sdk/0.0.1" />""");

            var result = await Run(block: root);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030401").And.Contains("Old.csproj: Umpire.Sdk 0.0.1"));
            Assert.That(result.Lines, Has.One.Contains("UMP030002: LoadApp skipped, because an earlier step failed."));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task With_no_block_the_engine_uses_the_nearest_check_entry_above_the_project() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");
            var project = Directory.CreateDirectory(Path.Combine(root, "App", "Host")).FullName;

            var result = await Run(project: project);

            Assert.That(result.Lines, Has.One.Contains("error UMP030501").And.Contains($"under the block folder {Path.GetFullPath(root)}"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task With_no_check_entry_and_no_block_the_run_gives_UMP030303_and_runs_no_other_step() {
        var project = TempFolder.Create();
        try {
            var result = await Run(project: project, bin: project);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030303: No check entry is in"));
            Assert.That(result.Lines, Has.None.Contains("UMP030002"));
        } finally {
            TempFolder.Delete(project);
        }
    }

    [TestCase("packs", "Vsa")]
    [TestCase("tests", "Fixtures", "Blocks", "Packs", "Alpha")]
    [TestCase("tests", "Fixtures", "PackVariant", "Packs", "Ecs")]
    public async Task A_pack_block_passes_its_own_check(params string[] folder) {
        var result = await Run(project: Repo.Path(folder));

        Assert.That(result.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task An_unmarked_block_in_a_part_folder_gives_UMP020401() {
        var store = Path.Combine(Notes, "Features", "Notebook", "Infrastructure", "Store");
        Directory.CreateDirectory(store);
        try {
            File.WriteAllText(Path.Combine(store, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");

            var result = await Run(project: Notes);

            Assert.That(result.Lines, Has.One.Contains("error UMP020401: Infrastructure (Features/{feature}/Infrastructure) covers "
                + "the folder Features/Notebook/Infrastructure/Store"));
        } finally {
            Directory.Delete(store, recursive: true);
        }
    }

    [Test]
    public async Task A_severity_of_none_in_the_umpireconfig_of_the_block_turns_a_rule_off() {
        var store = Path.Combine(Notes, "Features", "Notebook", "Infrastructure", "Store");
        var config = Path.Combine(Notes, ".umpireconfig");
        var original = File.ReadAllText(config);
        Directory.CreateDirectory(store);
        try {
            File.WriteAllText(Path.Combine(store, ".umpireconfig"), "umpire.architecture = \"Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa\"");
            File.WriteAllText(config, original + "umpire.UMP020401.severity = \"none\"\n");

            var result = await Run(project: Notes);

            Assert.That(result.Lines, Has.None.Contains("UMP020401").And.None.Contains("UMP030001"), string.Join("\n", result.Lines));
        } finally {
            File.WriteAllText(config, original);
            Directory.Delete(store, recursive: true);
        }
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_a_block_with_inner_blocks() {
        var result = await Run(project: Repo.Path("tests", "Fixtures", "Blocks"));

        Assert.That(result.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_an_app_with_its_own_pack_variant() {
        var result = await Run(project: Repo.Path("tests", "Fixtures", "PackVariant"));

        Assert.That(result.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task A_step_that_fails_skips_the_steps_that_use_its_output() {
        var result = await Run(block: Path.Combine(Notes, "Features", "Missing"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030501"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_with_two_app_classes_gives_UMP030503_with_both_names() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "TwoApps"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030503: The app has more than one app class: "
            + "TwoApps.App.Host.FirstApp, TwoApps.App.Host.SecondApp."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task A_launcher_entry_with_a_private_app_class_and_no_empty_constructor_loads_its_architecture() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "ShellApp"));

        Assert.That(result.Lines, Has.None.Contains("UMP0305").And.None.EqualTo(RulesSkipped), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task A_Unity_project_skips_the_build_reads_its_script_assemblies_and_skips_its_generated_projects() {
        var unity = TempFolder.Create();
        try {
            Directory.CreateDirectory(Path.Combine(unity, "ProjectSettings"));
            File.WriteAllText(Path.Combine(unity, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.6.0f1");
            Directory.CreateDirectory(Path.Combine(unity, "Library", "ScriptAssemblies"));
            File.WriteAllText(Path.Combine(unity, "Assembly-CSharp.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(unity, "Game.csproj"), "<Project />");

            var result = await Run(project: unity, bin: "", stage: Stage.All);

            Assert.That(result.Lines, Has.One.Contains("UMP030102: ").And.Contains("is a Unity project"), string.Join("\n", result.Lines));
            Assert.That(result.Lines, Has.None.Contains("UMP030101").And.None.Contains("UMP030305").And.None.Contains("UMP030306"));
            Assert.That(result.Lines, Has.None.Contains("UMP030001"), string.Join("\n", result.Lines));
        } finally {
            TempFolder.Delete(unity);
        }
    }

    [Test]
    public async Task An_app_with_no_app_class_gives_UMP030502() {
        var result = await Run(block: Repo.Path("tests", "Shared"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030502: The app has no app class."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task A_step_that_throws_gives_one_error_and_the_run_continues() {
        var result = await Run(bin: Path.Combine(Notes, "missing-bin"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.StartsWith("umpire-engine: error UMP030001: LoadApp could not run: "));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public void With_fail_fast_a_step_that_throws_stops_the_run() {
        var bin = Path.Combine(Notes, "missing-bin");

        Assert.ThrowsAsync<DirectoryNotFoundException>(() => Run(bin: bin, failFast: true));
    }

    [Test]
    public async Task A_umpireconfig_file_that_is_not_TOML_stops_the_run_with_the_line_of_the_error() {
        var root = TempFolder.Create();
        try {
            var file = Path.Combine(root, ".umpireconfig");
            File.WriteAllText(file, "umpire.UMP010101.max = 60\n[*.cs]\n");

            var result = await Run(block: root);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.StartsWith("umpire-engine: error UMP030001: FindBlocks could not run: ")
                .And.Contains($"{Path.GetFullPath(file)}(2,"));
            Assert.That(result.Lines, Has.None.Contains("UMP030002"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task The_report_line_has_the_line_and_the_column_of_the_finding() {
        using var host = new EngineTestApp(new StepsPlugin(), new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var finding = new Finding("UMP020101", FindingSeverity.Error, "Probe in Host", "Probe.cs", 4, 9);

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new WriteReportCommand(ReportMode.Error, [finding]), default);

        Assert.That(result.Value.Lines, Is.EqualTo(new[] { "Probe.cs(4,9): error UMP020101: Probe in Host" }));
    }

    internal static async Task<RunEngineResponse> Run(
        string? block = null, string? bin = null, bool failFast = false, string? project = null,
        Stage stage = Stage.Check, bool keepStamp = false) {
        using var host = new EngineTestApp(
            new StepsPlugin(), new EngineModule(), new BuildModule(), new ChangesModule(), new BlocksModule(), new VersionsModule(),
            new LoadAppModule(), new RulesModule(), new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var declared = project is null ? block ?? Notes : "";
        var output = bin ?? AppContext.BaseDirectory;
        if (Directory.Exists(output) && keepStamp is false) {
            StampFile.Delete(output);
        }
        var request = new RunEngineRequest(project ?? declared, output, declared, FailFast: failFast, Stage: stage);
        var result = await scope.ServiceProvider.GetRequiredService<IRequestSender>().SendAsync(request, default);
        return result.Value;
    }
}
