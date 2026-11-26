namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public class AppHost : IDisposable {
    ServiceProvider? provider;

    public IModuleProvider? ModuleProvider { get; init; }

    public virtual IServiceProvider Build(string[] args, AppParts app) {
        if (provider is not null) {
            throw new InvalidOperationException($"This {GetType().Name} is in use. Give a new host to each Start.");
        }
        var services = new ServiceCollection();
        Compose(services, app);
        provider = services.BuildServiceProvider();
        return provider;
    }

    public virtual void Start(IServiceProvider services, AppStop stop) {
    }

    public virtual void Dispose() {
        provider?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Compose(IServiceCollection services, AppParts app) {
        var types = new AppTypes(ModuleProvider ?? new ScannedModules(app.Assembly));
        Core.Install(services, types);
        Install(services, types);
        app.Kernel.Install(services, types);
        foreach (var module in types.Modules) {
            module.Install(services);
        }
    }

    protected virtual void Install(IServiceCollection services, AppTypes types) {
    }
}
