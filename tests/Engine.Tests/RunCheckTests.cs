using Check;
using Check.Slices.RunCheck;
using Definition;
using Hello;
using Microsoft.Extensions.DependencyInjection;
using Report;
using Rules;
using Tests.Shared;
using VSA.Composition;
using VSA.Contracts.Commands;

namespace Engine.Tests;

public sealed class RunCheckTests {
    static readonly string Notes = Repo.Path("samples", "Notes");

    [Test]
    public async Task The_engine_finds_no_errors_in_Notes() {
        var result = await Run();

        Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
        Assert.That(result.Lines, Has.None.Contains(": error "));
    }

    [Test]
    public async Task The_Hello_plugin_adds_its_line_when_it_is_on() {
        var result = await Run();

        Assert.That(result.Lines, Has.One.EqualTo("vsa-engine: HELLO: Hello from a plugin"));
    }

    [Test]
    public async Task The_Hello_plugin_does_not_run_when_it_is_off() {
        var result = await Run(off: "Hello");

        Assert.That(result.Lines, Has.None.Contains("HELLO"));
    }

    [Test]
    public async Task An_app_folder_with_no_app_assembly_gives_one_error() {
        var result = await Run(root: Path.Combine(Notes, "Features", "Missing"));

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error VSA3002"));
    }

    static async Task<RunCheckResult> Run(string? root = null, string off = "") {
        using var host = VsaBuilder.Create()
            .Modules(new CheckModule(), new DefinitionModule(), new RulesModule(), new ReportModule(), new HelloModule())
            .UseGenericHost()
            .Build();
        await using var scope = host.Services.CreateAsyncScope();
        var command = new RunCheckCommand(root ?? Notes, AppContext.BaseDirectory, Off: off);
        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);
        return result.Value;
    }
}
