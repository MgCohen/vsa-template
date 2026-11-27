namespace Framework.Tests.Host;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Composition;

sealed class WebTestApp : App {
    readonly List<Action<IServiceCollection>> configure = [];
    readonly Module[] modules;

    public WebTestApp(params Module[] modules) => this.modules = modules;

    public override Kernel Kernel { get; } = new VsaKernel();

    public WebTestApp With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public WebRun Build(params string[] args) {
        var host = CreateHost(args);
        var services = host.Build(this);
        host.Start(services, services.GetRequiredService<AppStop>());
        return new WebRun(host, services);
    }

    public Task<WebRun> Start() => Task.FromResult(Build());

    public AppInstance RunOnTestServer() => Run(CreateHost([]));

    public int RunToEnd() => RunOnTestServer().Completion.GetAwaiter().GetResult();

    public IServiceCollection Registrations() {
        IServiceCollection? registrations = null;
        using (With(services => registrations = services).Build()) {
        }
        return registrations!;
    }

    WebTestHost CreateHost(string[] args) {
        var host = new WebTestHost(args) { ModuleProvider = new FixedModules(modules) };
        host.Actions.AddRange(configure);
        return host;
    }
}
