using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using VSA.Architecture;
using VSA.Architecture.Vsa;
using Microsoft.Extensions.Configuration;
using Vsa = VSA.Architecture.Vsa;
using VSA.Composition;
using VSA.Kernel;

namespace Framework.Tests.Host;

public sealed class VsaAppTests {
    [Test]
    public void Run_makes_the_app_and_runs_its_start_steps() {
        var app = new StartedApp();

        var exitCode = app.Run(["--name", "test"]);

        Assert.That((exitCode, app.Marker), Is.EqualTo((7, "test")));
    }

    [Test]
    public async Task A_waiting_host_returns_from_Run_after_Stop_and_the_app_can_run_again() {
        var app = new WaitingApp();
        await RunUntilStop(app);

        Assert.That(await RunUntilStop(app), Is.Zero);
    }

    [Test]
    public void A_returning_host_returns_from_Run_and_Stop_stops_the_app() {
        var app = new ReturningApp();
        var exitCode = app.Run([]);
        var runningAfterRun = app.IsRunning;
        app.Stop();

        Assert.That((exitCode, runningAfterRun, app.IsRunning), Is.EqualTo((0, true, false)));
    }

    [Test]
    public void Run_after_Stop_starts_the_app_again() {
        var app = new ReturningApp();
        app.Run([]);
        app.Stop();

        app.Run([]);
        var running = app.IsRunning;
        app.Stop();

        Assert.That(running, Is.True);
    }

    [Test]
    public void Run_of_a_running_app_is_an_error() {
        var app = new ReturningApp();
        app.Run([]);
        try {
            var error = Assert.Throws<InvalidOperationException>(() => app.Run([]));

            Assert.That(error!.Message, Is.EqualTo("ReturningApp is already running. Call Stop() before Run."));
        } finally {
            app.Stop();
        }
    }

    [Test]
    public void Stop_of_an_app_that_does_not_run_does_nothing() {
        Assert.DoesNotThrow(new ReturningApp().Stop);
    }

    [Test]
    public void The_default_host_is_the_generic_host() {
        Assert.That(new PlainApp().DefaultHost, Is.TypeOf<GenericAppHost>());
    }

    [Test]
    public void An_app_of_the_VSA_definition_has_the_VSA_rules() {
        Assert.That(Rules(new PlainApp().Architecture), Is.EqualTo(Rules(new VsaDefinition())));
    }

    [Test]
    public void An_app_of_a_derived_definition_has_its_rules_after_the_VSA_rules() {
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

    static async Task<int> RunUntilStop(WaitingApp app) {
        var run = Task.Run(() => app.Run([]));
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (!app.IsRunning && !run.IsCompleted && DateTime.UtcNow < deadline) {
            await Task.Delay(10);
        }
        app.Stop();
        Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))), Is.SameAs(run));
        Assert.That(app.IsRunning, Is.False);
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

    sealed class PlainApp : VsaApp<VsaDefinition> {
        public IAppHost DefaultHost => Host();
    }

    sealed class RulesDefinition : VsaDefinition {
        protected override void Rules(RuleBook rules) {
            base.Rules(rules);
            rules.For<Vsa.Presenters>().Denies<Vsa.Contracts.Events>();
        }
    }

    sealed class RulesApp : VsaApp<RulesDefinition>;

    sealed class StartedApp : VsaApp<VsaDefinition> {
        public string? Marker { get; private set; }

        protected override int Start(string[] args) {
            using var host = Build(args);
            Marker = host.Services.GetRequiredService<IConfiguration>()["name"];
            return host.Services.GetRequiredService<ExitCode>().Value;
        }

        protected override IReadOnlyList<VSA.Module.Module> Modules() => [];

        protected override void Services(IServiceCollection services) => services.AddSingleton(new ExitCode(7));
    }

    sealed class WaitingApp : VsaApp<VsaDefinition> {
        protected override IReadOnlyList<VSA.Module.Module> Modules() => [];
    }

    sealed class ReturningApp : VsaApp<VsaDefinition> {
        protected override IAppHost Host() => new ReturningHost();

        protected override IReadOnlyList<VSA.Module.Module> Modules() => [];
    }

    sealed record ExitCode(int Value);
}
