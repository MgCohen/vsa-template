namespace Framework.Tests.Host;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Architecture;
using Umpire.Hosts.Web;
using Umpire.Kernels;
using Umpire.Vsa;
using Umpire.Vsa.Definition;
using Concepts = Umpire.Vsa.Definition;

public sealed class AppTests {
    [Test]
    public void Start_builds_the_services_and_calls_OnStart_with_them() {
        var app = new LifecycleApp();

        app.Start([]);
        app.Stop();

        Assert.That(app.TypesOnStart, Is.Not.Null);
    }

    [Test]
    public async Task Run_waits_until_Stop_and_the_app_can_run_again() {
        var app = new LifecycleApp();
        await RunUntilStop(app);

        Assert.That(await RunUntilStop(app), Is.Zero);
    }

    [Test]
    public void Stop_in_OnStart_makes_Run_return_and_the_app_can_run_again() {
        var app = new LifecycleApp { StopOnStart = true };

        var first = app.Run([]);
        var second = app.Run([]);

        Assert.That(first, Is.Zero);
        Assert.That(second, Is.Zero);
        Assert.That(app.State == AppState.Running, Is.False);
        Assert.That(app.Stops, Is.EqualTo(2));
    }

    [Test]
    public void Start_returns_and_Stop_stops_the_app() {
        var app = new LifecycleApp();
        app.Start([]);
        var runningAfterStart = app.State == AppState.Running;
        app.Stop();

        Assert.That(runningAfterStart, Is.True);
        Assert.That(app.State == AppState.Running, Is.False);
        Assert.That(app.Services, Is.Null);
    }

    [Test]
    public void OnStop_runs_when_the_app_stops() {
        var app = new LifecycleApp();
        app.Start([]);

        app.Stop();

        Assert.That(app.Stops, Is.EqualTo(1));
    }

    [Test]
    public void Start_after_Stop_starts_the_app_again() {
        var app = new LifecycleApp();
        app.Start([]);
        app.Stop();

        app.Start([]);
        var running = app.State == AppState.Running;
        app.Stop();

        Assert.That(running, Is.True);
        Assert.That(app.Stops, Is.EqualTo(2));
    }

    [Test]
    public void Start_of_a_running_app_is_an_error() {
        var app = new LifecycleApp();
        app.Start([]);
        try {
            var error = Assert.Throws<InvalidOperationException>(() => app.Start([]));

            Assert.That(error!.Message, Is.EqualTo("LifecycleApp is already running. Call Stop() before Start or Run."));
        } finally {
            app.Stop();
        }
    }

    [Test]
    public void Stop_of_an_app_that_does_not_run_does_nothing() {
        Assert.DoesNotThrow(new LifecycleApp().Stop);
    }

    [Test]
    public void Dispose_of_a_host_that_did_not_build_does_nothing() {
        Assert.DoesNotThrow(new AppHost().Dispose);
    }

    [Test]
    public void The_pack_gives_a_new_host_of_its_host_type_on_each_call() {
        var pack = new WebApp().Pack;

        var first = pack.CreateHost();

        Assert.That(first, Is.TypeOf<WebHost>());
        Assert.That(pack.CreateHost(), Is.Not.SameAs(first));
    }

    [Test]
    public void The_VSA_pack_names_the_VSA_kernel() {
        Assert.That(new WebApp().Pack.Kernel, Is.TypeOf<Umpire.Vsa.Composition.VsaKernel>());
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
    [TestCase("Microsoft.Extensions.Hosting")]
    public void The_runtime_of_an_app_does_not_reference_the_test_tools_or_the_generic_host(string tool) {
        Assert.That(RuntimeReferences(typeof(Vsa<>).Assembly), Has.None.StartsWith(tool));
    }

    static async Task<int> RunUntilStop(LifecycleApp app) {
        var run = Task.Run(() => app.Run([]));
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (app.State != AppState.Running && !run.IsCompleted && DateTime.UtcNow < deadline) {
            await Task.Delay(10);
        }
        app.Stop();
        Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))), Is.SameAs(run));
        Assert.That(app.State == AppState.Running, Is.False);
        return await run;
    }

    static List<Rule> Rules(Architecture definition) =>
        [.. new ResolvedArchitecture(definition).UseRules.Select(p => new Rule(p.From.Name, p.To.Name, p.Permission))];

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

    sealed class WebApp : App<Vsa<WebHost>>;

    sealed class RulesArchitecture : VsaArchitecture {
        protected override void Rules(RuleBook rules) {
            rules.For<Concepts.Presenters>().Denies<Concepts.Contracts.Events>();
        }
    }

    sealed class RulesApp : App<Vsa<RulesArchitecture, WebHost>>;

    sealed class LifecycleApp : App {
        public override Pack Pack { get; } = new TestPack(() => new AppHost { ModuleProvider = new FixedModules() });

        public bool StopOnStart { get; init; }

        public AppTypes? TypesOnStart { get; private set; }

        public int Stops { get; private set; }

        protected override void OnStart() {
            TypesOnStart = Services!.GetRequiredService<AppTypes>();
            if (StopOnStart) {
                Stop();
            }
        }

        protected override void OnStop() => Stops++;
    }
}
