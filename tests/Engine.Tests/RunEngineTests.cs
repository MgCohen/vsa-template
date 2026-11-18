using Blocks;
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

        Assert.That(modules, Is.EqualTo(new[] { "BuiltApp", "Steps", "Blocks", "Engine", "LoadApp", "Report", "Rules", "Versions" }));
    }

    [Test]
    public void The_engine_reads_the_architecture_from_the_pack_of_the_app_class() {
        var app = new TypeName(typeof(EngineApp).FullName!, typeof(EngineApp).Assembly.GetName().Name!);

        var architecture = new AppLoader(AppContext.BaseDirectory).ArchitectureOfApp(app, typeof(EngineApp).Assembly.Location);

        Assert.That(architecture.GetType().FullName, Is.EqualTo(typeof(EngineArchitecture).FullName));
    }

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
    public async Task An_Umpire_version_that_is_not_the_engine_version_gives_UMP030201_and_skips_the_load() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "Old.csproj"), """<Project Sdk="Umpire.Sdk/0.0.1" />""");

            var result = await Run(block: root);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030201").And.Contains("Old.csproj: Umpire.Sdk 0.0.1"));
            Assert.That(result.Lines, Has.One.Contains("UMP030002: LoadApp skipped, because an earlier step failed."));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task With_no_block_the_engine_uses_the_nearest_check_entry_above_the_project() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, ".umpireconfig"), "umpire.architecture = Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa");
            var project = Directory.CreateDirectory(Path.Combine(root, "App", "Host")).FullName;

            var result = await Run(project: project);

            Assert.That(result.Lines, Has.One.Contains("error UMP030301").And.Contains($"under the block folder {Path.GetFullPath(root)}"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task With_no_check_entry_and_no_block_the_run_gives_UMP030103_and_runs_no_other_step() {
        var project = TempFolder.Create();
        try {
            var result = await Run(project: project, bin: project);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030103: No check entry is in"));
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
            File.WriteAllText(Path.Combine(store, ".umpireconfig"), "umpire.architecture = Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa");

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
            File.WriteAllText(Path.Combine(store, ".umpireconfig"), "umpire.architecture = Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa");
            File.WriteAllText(config, original + "dotnet_diagnostic.UMP020401.severity = none\n");

            var result = await Run(project: Notes);

            Assert.That(result.Lines, Has.None.Contains("UMP020401"));
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
        Assert.That(result.Lines, Has.One.Contains("error UMP030301"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_with_two_app_classes_gives_UMP030303_with_both_names() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "TwoApps"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030303: The app has more than one app class: "
            + "TwoApps.App.Host.FirstApp, TwoApps.App.Host.SecondApp."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_class_with_no_empty_constructor_gives_UMP030304_with_its_name() {
        var result = await Run(block: Repo.Path("tests", "Fixtures", "NoConstructorApp"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030304: The app class NoConstructorApp.App.Host.NamedApp "
            + "has no public constructor with no parameters"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_with_no_app_class_gives_UMP030302() {
        var result = await Run(block: Repo.Path("tests", "Shared"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030302: The app has no app class."));
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
    public async Task The_report_line_has_the_line_and_the_column_of_the_finding() {
        using var host = new EngineTestApp(new StepsPlugin(), new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var finding = new Finding("UMP020101", FindingSeverity.Error, "Probe in Host", "Probe.cs", 4, 9);

        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new WriteReportCommand(ReportMode.Error, [finding]), default);

        Assert.That(result.Value.Lines, Is.EqualTo(new[] { "Probe.cs(4,9): error UMP020101: Probe in Host" }));
    }

    static async Task<RunEngineResponse> Run(
        string? block = null, string? bin = null, bool failFast = false, string? project = null) {
        using var host = new EngineTestApp(
            new StepsPlugin(), new EngineModule(), new BlocksModule(), new VersionsModule(), new LoadAppModule(), new RulesModule(),
            new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var declared = project is null ? block ?? Notes : "";
        var output = bin ?? AppContext.BaseDirectory;
        var request = new RunEngineRequest(project ?? declared, output, declared, FailFast: failFast);
        var result = await scope.ServiceProvider.GetRequiredService<IRequestSender>().SendAsync(request, default);
        return result.Value;
    }
}
