namespace Framework.Tests.Host;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Host;
using VSA.Kernel;

sealed class WebTestApp(params Module[] modules) : App<Vsa<WebVsaHost>> {
    readonly List<Action<IServiceCollection>> configure = [];

    public WebTestApp With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public IHost Build() => Pack.CreateHost().Create([], this);

    public async Task<IHost> Start() {
        var host = Build();
        await host.StartAsync();
        return host;
    }

    public IServiceCollection Registrations() {
        IServiceCollection? registrations = null;
        With(services => registrations = services).Build().Dispose();
        return registrations!;
    }

    protected override IReadOnlyList<Module> Modules() => modules;

    protected override void ConfigureServices(IServiceCollection services) {
        services.AddSingleton<IServer, TestServer>();
        foreach (var add in configure) {
            add(services);
        }
    }
}
