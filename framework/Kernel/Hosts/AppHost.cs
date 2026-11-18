namespace Umpire.Kernels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Markers;

[RequireDerived]
public class AppHost : IDisposable {
    ServiceProvider? provider;

    public IReadOnlyList<Module>? Modules { get; init; }

    public virtual IServiceProvider Build(string[] args, App app) {
        var services = new ServiceCollection();
        Compose(services, new ConfigurationBuilder().Build(), app);
        provider = services.BuildServiceProvider();
        return provider;
    }

    public virtual void Dispose() {
        provider?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        provider = null;
        GC.SuppressFinalize(this);
    }

    public void Compose(IServiceCollection services, IConfiguration configuration, Kernel kernel, AppTypes types) {
        kernel.Install(services, types, configuration);
        foreach (var module in types.Modules) {
            module.Compose(services, configuration);
        }
        Configure(services, types);
    }

    protected void Compose(IServiceCollection services, IConfiguration configuration, App app) =>
        Compose(services, configuration, app.Pack.Kernel, TypesOf(app));

    protected AppTypes TypesOf(App app) => new(Modules, app.GetType().Assembly);

    protected virtual void Configure(IServiceCollection services, AppTypes types) {
    }
}
