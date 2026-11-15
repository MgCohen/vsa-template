namespace Framework.Tests.Host;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Architecture;
using Umpire.Vsa;
using Umpire.Vsa.Definition;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Hosts.Web;
using Umpire.Kernel;
using Concepts = Umpire.Vsa.Definition;

public sealed class AppTests {
    [Test]
    public void Run_composes_the_app_and_calls_OnStart_with_the_services() {
        var app = new ReturningApp();

        var exitCode = app.Run(["--name", "test"]);
        app.Stop();

        Assert.That(exitCode, Is.Zero);
        Assert.That(app.Marker, Is.EqualTo("test"));
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
        app.Run([]);
        var runningAfterRun = app.IsRunning;
        app.Stop();

        Assert.That(runningAfterRun, Is.True);
        Assert.That(app.IsRunning, Is.False);
        Assert.That(app.Services, Is.Null);
    }

    [Test]
    public void OnStop_runs_when_the_host_stops() {
        var app = new ReturningApp();
        app.Run([]);

        app.Stop();

        Assert.That(app.Stops, Is.EqualTo(1));
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
        Assert.That(app.Stops, Is.EqualTo(2));
    }

    [Test]
    public void Run_of_a_running_app_is_an_error() {
        var app = new ReturningApp();
        app.Run([]);
        try {
            var error = Assert.Throws<InvalidOperationException>(() => app.Run([]));

            Assert.That(error!.Message, Is.EqualTo("ReturningApp is already running. Call Stop() before Run."));
            Assert.That(app.Composes, Is.EqualTo(1));
        } finally {
            app.Stop();
        }
    }

    [Test]
    public void Stop_of_an_app_that_does_not_run_does_nothing() {
        Assert.DoesNotThrow(new ReturningApp().Stop);
    }

    [Test]
    public void The_pack_gives_a_new_host_of_its_host_type_on_each_call() {
        var pack = new WebApp().Pack;

        var first = pack.CreateHost();

        Assert.That(first, Is.TypeOf<WebVsaHost>());
        Assert.That(pack.CreateHost(), Is.Not.SameAs(first));
    }

    [Test]
    public void An_app_of_the_VSA_pack_has_the_VSA_rules() {
        Assert.That(Rules(new WebApp().Pack.Architecture), Is.EqualTo(Rules(new VsaArchitecture())));
    }

    [Test]
    public void An_app_of_a_derived_architecture_has_its_rules_after_the_VSA_rules() {
        var app = Rules(new RulesApp().Pack.Architecture);
        var vsa = Rules(new VsaArchitecture());

        Assert.That(app.Take(vsa.Count), Is.EqualTo(vsa));
        Assert.That(app[^1], Is.EqualTo(new Rule("Presenters", "Contracts.Events", Permission.Deny)));
    }

    [TestCase("ArchUnitNET")]
    [TestCase("Mono.Cecil")]
    [TestCase("nunit.framework")]
    public void The_runtime_of_an_app_does_not_reference_the_test_tools(string tool) {
        Assert.That(RuntimeReferences(typeof(Vsa<>).Assembly), Has.None.StartsWith(tool));
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

    static List<Rule> Rules(Architecture definition) =>
        [.. definition.Resolve().Select(p => new Rule(p.From.Name, p.To.Name, p.Permission))];

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

    sealed record Rule(string From, string To, Permission Permission);

    sealed class WebApp : App<Vsa<WebVsaHost>>;

    sealed class RulesArchitecture : VsaArchitecture {
        protected override void Rules(RuleBook rules) {
            base.Rules(rules);
            rules.For<Concepts.Presenters>().Denies<Concepts.Contracts.Events>();
        }
    }

    sealed class RulesApp : App<Vsa<RulesArchitecture, WebVsaHost>>;

    sealed class WaitingApp : App<Vsa<VsaHost>> {
        protected override IReadOnlyList<Umpire.Kernel.Module> Modules() => [];
    }

    sealed class ReturningApp : App<Vsa<ReturningHost>> {
        public string? Marker { get; private set; }

        public int Stops { get; private set; }

        public int Composes { get; private set; }

        protected override IReadOnlyList<Umpire.Kernel.Module> Modules() => [];

        protected override void OnStart() => Marker = Services!.GetRequiredService<IConfiguration>()["name"];

        protected override void OnStop() => Stops++;

        protected override void ConfigureServices(IServiceCollection services) => Composes++;
    }
}
