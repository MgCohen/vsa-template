using Definition;
using Engine.Slices.RunEngine;
using Hello;
using Microsoft.Extensions.DependencyInjection;
using Report;
using Rules;
using Steps;
using Steps.Domain.Shared;
using Tests.Shared;
using VSA.Composition;
using VSA.Contracts.Commands;

namespace Engine.Tests;

public sealed class RunEngineTests {
    const string Hello = "vsa-engine: HELLO: Hello from a plugin";
    const string RulesSkipped =
        "vsa-engine: VSA3004: CheckRules skipped: App is missing, because an earlier step failed.";
    static readonly string Notes = Repo.Path("samples", "Notes");

    [Test]
    public async Task The_engine_finds_no_errors_in_Notes() {
        var result = await Run();

        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.Contains(": error "));
    }

    [Test]
    public async Task The_Hello_plugin_adds_its_line() {
        var result = await Run();

        Assert.That(result.Lines, Has.One.EqualTo(Hello));
    }

    [Test]
    public async Task A_step_that_fails_skips_the_steps_that_use_its_output_and_the_others_run() {
        var result = await Run(root: Path.Combine(Notes, "Features", "Missing"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error VSA3002"));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
        Assert.That(result.Lines, Has.One.EqualTo(Hello));
    }

    [Test]
    public async Task A_step_that_throws_gives_one_error_and_the_run_continues() {
        var result = await Run(bin: Path.Combine(Notes, "missing-bin"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.StartsWith("vsa-engine: error VSA3000: LoadDefinition could not run: "));
        Assert.That(result.Lines, Has.One.EqualTo(RulesSkipped));
        Assert.That(result.Lines, Has.One.EqualTo(Hello));
    }

    [Test]
    public void With_fail_fast_a_step_that_throws_stops_the_run() {
        var bin = Path.Combine(Notes, "missing-bin");

        Assert.ThrowsAsync<DirectoryNotFoundException>(() => Run(bin: bin, failFast: true));
    }

    static async Task<ReportResult> Run(string? root = null, string? bin = null, bool failFast = false) {
        VSA.Module.Module[] modules = [
            new StepsPlugin(), new EngineModule(), new DefinitionModule(),
            new RulesModule(), new ReportModule(), new HelloModule(),
        ];
        using var host = VsaBuilder.Create().Modules(modules).UseGenericHost().Build();
        await using var scope = host.Services.CreateAsyncScope();
        var command = new RunEngineCommand(root ?? Notes, bin ?? AppContext.BaseDirectory, FailFast: failFast);
        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);
        return result.Value;
    }
}
