namespace Framework.Tests.Host;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Architecture;
using Umpire.Hosts.Web;
using Umpire.Kernels;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Definition;

public sealed class AppTests {
    [Test]
    public void Start_builds_the_services_and_calls_OnStart_with_them() {
        var app = new LifecycleApp();

        app.Start();
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

        var first = app.RunToEnd();
        var second = app.RunToEnd();

        Assert.That(first, Is.Zero);
        Assert.That(second, Is.Zero);
        Assert.That(app.State == AppState.Running, Is.False);
        Assert.That(app.Stops, Is.EqualTo(2));
    }

    [Test]
    public void Start_returns_and_Stop_stops_the_app() {
        var app = new LifecycleApp();
        app.Start();
        var runningAfterStart = app.State == AppState.Running;
        app.Stop();

        Assert.That(runningAfterStart, Is.True);
        Assert.That(app.State == AppState.Running, Is.False);
        Assert.That(app.Services, Is.Null);
    }

    [Test]
    public void Only_the_app_class_and_the_framework_read_the_services_of_an_app() {
        var services = typeof(App).GetProperty(nameof(App.Services), BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.That(services.GetMethod!.IsFamilyOrAssembly, Is.True);
        Assert.That(typeof(IApp).GetProperty(nameof(App.Services)), Is.Null);
    }

    [Test]
    public void OnStop_runs_when_the_app_stops() {
        var app = new LifecycleApp();
        app.Start();

        app.Stop();

        Assert.That(app.Stops, Is.EqualTo(1));
    }

    [Test]
    public void Start_after_Stop_starts_the_app_again() {
        var app = new LifecycleApp();
        app.Start();
        app.Stop();

        app.Start();
        var running = app.State == AppState.Running;
        app.Stop();

        Assert.That(running, Is.True);
        Assert.That(app.Stops, Is.EqualTo(2));
    }

    [Test]
    public void Start_of_a_running_app_is_an_error() {
        var app = new LifecycleApp();
        app.Start();
        try {
            var error = Assert.Throws<InvalidOperationException>(() => app.Start());

            Assert.That(error!.Message, Is.EqualTo("LifecycleApp is already running. Stop its AppInstance before the next Run."));
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
    public async Task The_instance_completes_with_the_exit_code_of_its_stop() {
        var app = new LifecycleApp();

        var running = app.Start();
        running.Stop(3);

        Assert.That(await running, Is.EqualTo(3));
        Assert.That(running.State, Is.EqualTo(AppState.Stopped));
    }

    [Test]
    public void An_old_instance_does_not_stop_the_next_run() {
        var app = new LifecycleApp();
        var first = app.Start();
        first.Stop();
        var second = app.Start();

        first.Stop();

        Assert.That(second.State, Is.EqualTo(AppState.Running));
        second.Stop();
    }

    [Test]
    public void Run_with_no_host_is_an_error() {
        Assert.Throws<ArgumentNullException>(() => new LifecycleApp().Run(null!));
    }

    [Test]
    public void The_app_gives_its_kernel() {
        Assert.That(new WebApp().Kernel, Is.TypeOf<VsaKernel>());
    }

    [TestCase("ArchUnitNET")]
    [TestCase("Mono.Cecil")]
    [TestCase("nunit.framework")]
    [TestCase("Microsoft.Extensions.Hosting")]
    public void The_runtime_of_an_app_does_not_reference_the_test_tools_or_the_generic_host(string tool) {
        Assert.That(RuntimeReferences(typeof(VsaArchitecture).Assembly), Has.None.StartsWith(tool));
    }

    [TestCase(typeof(App))]
    [TestCase(typeof(VsaKernel))]
    [TestCase(typeof(WebHost))]
    public void The_runtime_does_not_reference_a_definition(Type runtime) {
        var references = RuntimeReferences(runtime.Assembly);

        Assert.That(references, Does.Not.Contain("Umpire.Kernels.Definition"));
        Assert.That(references, Does.Not.Contain("Umpire.Vsa"));
        Assert.That(references, Does.Not.Contain("Umpire.Architecture"));
    }

    [Test]
    public void The_definition_api_does_not_reference_the_kernel() {
        var references = typeof(Architecture).Assembly.GetReferencedAssemblies().Select(reference => reference.Name);

        Assert.That(references, Does.Not.Contain("Umpire.Kernels"));
    }

    static async Task<int> RunUntilStop(LifecycleApp app) {
        var run = Task.Run(() => app.RunToEnd());
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (app.State != AppState.Running && !run.IsCompleted && DateTime.UtcNow < deadline) {
            await Task.Delay(10);
        }
        app.Stop();
        Assert.That(await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(30))), Is.SameAs(run));
        Assert.That(app.State == AppState.Running, Is.False);
        return await run;
    }

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

    sealed class WebApp : App<VsaKernel>;

    sealed class LifecycleApp() : VsaTestApp(() => new AppHost { ModuleProvider = new FixedModules() }) {
        public bool StopOnStart { get; init; }

        public AppTypes? TypesOnStart { get; private set; }

        public int Stops { get; private set; }

        protected override void OnStart() {
            TypesOnStart = Services!.GetRequiredService<AppTypes>();
            if (StopOnStart) {
                Services!.GetRequiredService<AppStop>().Request(0);
            }
        }

        protected override void OnStop() => Stops++;
    }
}
