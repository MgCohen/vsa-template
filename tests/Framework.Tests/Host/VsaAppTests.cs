using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VSA.Architecture;
using VSA.Architecture.Vsa;
using Microsoft.Extensions.Configuration;
using Vsa = VSA.Architecture.Vsa;
using VSA.Composition;

namespace Framework.Tests.Host;

public sealed class VsaAppTests {
    [Test]
    public void Run_makes_the_app_and_runs_its_start_steps() {
        var exitCode = StartedApp.Run(["--name", "test"]);

        Assert.That((exitCode, StartedApp.Marker), Is.EqualTo((7, "test")));
    }

    [Test]
    public async Task A_waiting_host_returns_from_Run_after_Stop_and_the_app_can_run_again() {
        await RunUntilStop();

        Assert.That(await RunUntilStop(), Is.Zero);
    }

    [Test]
    public void A_returning_host_returns_from_Run_and_Stop_stops_the_app() {
        var exitCode = ReturningApp.Run([]);
        var runningAfterRun = ReturningApp.IsRunning;
        ReturningApp.Stop();

        Assert.That((exitCode, runningAfterRun, ReturningApp.IsRunning), Is.EqualTo((0, true, false)));
    }

    [Test]
    public void Run_after_Stop_starts_the_app_again() {
        ReturningApp.Run([]);
        ReturningApp.Stop();

        ReturningApp.Run([]);
        var running = ReturningApp.IsRunning;
        ReturningApp.Stop();

        Assert.That(running, Is.True);
    }

    [Test]
    public void Run_of_a_running_app_is_an_error() {
        ReturningApp.Run([]);
        try {
            var error = Assert.Throws<InvalidOperationException>(() => ReturningApp.Run([]));

            Assert.That(error!.Message, Is.EqualTo("ReturningApp is already running. Call ReturningApp.Stop() before Run."));
        } finally {
            ReturningApp.Stop();
        }
    }

    [Test]
    public void Stop_of_an_app_that_does_not_run_does_nothing() {
        Assert.DoesNotThrow(ReturningApp.Stop);
    }

    [Test]
    public void An_app_with_no_overrides_has_the_VSA_rules() {
        Assert.That(Rules(new PlainApp().Architecture), Is.EqualTo(Rules(new VsaDefinition())));
    }

    [Test]
    public void A_Rules_override_adds_its_rules_after_the_VSA_rules() {
        var app = Rules(new RulesApp().Architecture);
        var vsa = Rules(new VsaDefinition());

        Assert.That(app.Take(vsa.Count), Is.EqualTo(vsa));
        Assert.That(app[^1], Is.EqualTo(("Presenters", "Contracts.Events", Permission.Deny)));
    }

    [TestCase("ArchUnitNET")]
    [TestCase("Mono.Cecil")]
    [TestCase("nunit.framework")]
    public void The_runtime_of_an_app_does_not_reference_the_test_tools(string tool) {
        Assert.That(RuntimeReferences(typeof(VsaApp<>).Assembly), Has.None.StartsWith(tool));
    }

    static async Task<int> RunUntilStop() {
        var run = Task.Run(() => WaitingApp.Run([]));
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!WaitingApp.IsRunning && !run.IsCompleted && DateTime.UtcNow < deadline) {
            await Task.Delay(10);
        }
        WaitingApp.Stop();
        Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))), Is.SameAs(run));
        Assert.That(WaitingApp.IsRunning, Is.False);
        return await run;
    }

    static List<(string From, string To, Permission Permission)> Rules(ArchitectureDefinition definition) =>
        definition.Resolve().Select(p => (p.From.Name, p.To.Name, p.Permission)).ToList();

    static HashSet<string> RuntimeReferences(Assembly start) {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var next = new Queue<AssemblyName>([start.GetName()]);
        while (next.TryDequeue(out var name)) {
            if (!found.Add(name.Name!)) {
                continue;
            }
            foreach (var reference in Assembly.Load(name).GetReferencedAssemblies()) {
                next.Enqueue(reference);
            }
        }
        return found;
    }

    sealed class PlainApp : VsaApp<PlainApp>;

    sealed class RulesApp : VsaApp<RulesApp> {
        protected override void Rules(Map map) => map.For<Vsa.Presenters>().Denies<Vsa.Contracts.Events>();
    }

    sealed class StartedApp : VsaApp<StartedApp> {
        public static string? Marker { get; private set; }

        protected override int Start(string[] args) {
            using var host = Build(args);
            Marker = host.Services.GetRequiredService<IConfiguration>()["name"];
            return host.Services.GetRequiredService<ExitCode>().Value;
        }

        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).UseGenericHost().Modules();

        protected override void Services(IServiceCollection services) => services.AddSingleton(new ExitCode(7));
    }

    sealed class WaitingApp : VsaApp<WaitingApp> {
        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).UseGenericHost().Modules();
    }

    sealed class ReturningApp : VsaApp<ReturningApp> {
        protected override VsaBuilder Builder(string[] args) => VsaBuilder.Create(args).UseHost(new ReturningHost()).Modules();
    }

    sealed record ExitCode(int Value);
}
