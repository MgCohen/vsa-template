namespace Framework.Tests.Host;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

sealed class WebTestApp : App {
    readonly List<Action<IServiceCollection>> configure = [];
    readonly Module[] modules;

    public WebTestApp(params Module[] modules) {
        this.modules = modules;
        Pack = new TestPack(CreateHost);
    }

    public override Pack Pack { get; }

    public WebTestApp With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public WebRun Build(params string[] args) {
        var host = Pack.CreateHost();
        return new WebRun(host, host.Build(args, this));
    }

    public Task<WebRun> Start() => Task.FromResult(Build());

    public IServiceCollection Registrations() {
        IServiceCollection? registrations = null;
        using (With(services => registrations = services).Build()) {
        }
        return registrations!;
    }

    WebTestHost CreateHost() {
        var host = new WebTestHost { Modules = modules };
        host.Actions.AddRange(configure);
        return host;
    }
}
