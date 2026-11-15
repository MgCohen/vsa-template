using Baseline;
using LoadApp;
using LoadApp.Infrastructure;
using Engine.Slices.RunEngine;
using Microsoft.Extensions.DependencyInjection;
using Report;
using Report.Domain.Shared;
using Rules;
using Steps;
using Steps.Domain.Shared;
using Versions;
using Tests.Shared;
using VSA.Contracts.Commands;
using VSA.Kernel;
using VsaEngine;

namespace Engine.Tests;

public sealed class RunEngineTests {
    const string RulesSkipped =
        "vsa-engine: VSA3004: CheckRules skipped: App is missing, because an earlier step failed.";
    static readonly string Notes = Repo.Path("samples", "Notes");

    [Test]
    public void The_scan_finds_each_module_of_the_one_engine_assembly() {
        var engine = typeof(StepsPlugin).Assembly;

        var modules = AppTypes.InOrder(AppTypes.Roots().Where(root => root.Assembly == engine).Select(root => root.Create()))
            .Select(module => module.Name);

        Assert.That(modules, Is.EqualTo(new[] { "Steps", "Baseline", "Engine", "LoadApp", "Report", "Rules", "Versions" }));
    }

    [Test]
    public void The_engine_reads_the_architecture_from_the_pack_of_the_app_class() {
        var classes = DefinitionFinder.Find([typeof(EngineApp).Assembly]);

        Assert.That(classes.Usable, Is.EqualTo(new[] { typeof(EngineApp) }));
        Assert.That(DefinitionFinder.ArchitectureOf(classes.Usable[0]), Is.TypeOf<EngineArchitecture>());
    }

    [Test]
    public void Assemblies_with_no_app_class_give_no_architecture() {
        var classes = DefinitionFinder.Find([typeof(Repo).Assembly]);

        Assert.That(classes.Usable, Is.Empty);
        Assert.That(classes.NoConstructor, Is.Empty);
    }

    [Test]
    public async Task The_engine_finds_no_errors_in_Notes() {
        var result = await Run();

        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.Contains(": error "));
    }

    [Test]
    public async Task A_VSA_version_that_is_not_the_engine_version_gives_VSA3001_and_skips_the_load() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "global.json"), """{ "msbuild-sdks": { "VSA.Sdk": "0.0.1" } }""");

            var result = await Run(root: root);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error VSA3001").And.Contains("global.json: VSA.Sdk 0.0.1"));
            Assert.That(result.Lines, Has.One.Contains("VSA3004: LoadApp skipped: CheckedVersions is missing"));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public async Task A_step_that_fails_skips_the_steps_that_use_its_output() {
        var result = await Run(root: Path.Combine(Notes, "Features", "Missing"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error VSA3002"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_with_two_app_classes_gives_VSA3005_with_both_names() {
        var result = await Run(root: Repo.Path("tests", "Fixtures", "TwoApps"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error VSA3005: The app has more than one app class: "
            + "TwoApps.App.Host.FirstApp, TwoApps.App.Host.SecondApp."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_class_with_no_empty_constructor_gives_VSA3006_with_its_name() {
        var result = await Run(root: Repo.Path("tests", "Fixtures", "NoConstructorApp"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error VSA3006: The app class NoConstructorApp.App.Host.NamedApp "
            + "has no public constructor with no parameters"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task An_app_with_no_app_class_gives_VSA3003() {
        var result = await Run(root: Repo.Path("tests", "Shared"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error VSA3003: The app has no app class."));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public async Task A_step_that_throws_gives_one_error_and_the_run_continues() {
        var result = await Run(bin: Path.Combine(Notes, "missing-bin"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.StartsWith("vsa-engine: error VSA3000: LoadApp could not run: "));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
    }

    [Test]
    public void With_fail_fast_a_step_that_throws_stops_the_run() {
        var bin = Path.Combine(Notes, "missing-bin");

        Assert.ThrowsAsync<DirectoryNotFoundException>(() => Run(bin: bin, failFast: true));
    }

    static async Task<ReportResult> Run(string? root = null, string? bin = null, bool failFast = false) {
        using var host = new EngineTestApp(
            new StepsPlugin(), new EngineModule(), new VersionsModule(), new LoadAppModule(), new RulesModule(),
            new BaselineModule(), new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var command = new RunEngineCommand(root ?? Notes, bin ?? AppContext.BaseDirectory, FailFast: failFast);
        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);
        return result.Value;
    }
}
