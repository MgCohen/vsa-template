namespace Spike.Kernels;

using Microsoft.Extensions.DependencyInjection;

public class AppHost : IDisposable {
    ServiceProvider? provider;
    bool used;

    public IModuleProvider? ModuleProvider { get; init; }

    public virtual IServiceProvider Build(string[] args, AppParts app) {
        Claim();
        var services = new ServiceCollection();
        Compose(services, args, app);
        provider = services.BuildServiceProvider();
        return provider;
    }

    public virtual void Start(IServiceProvider services, AppStop stop) {
    }

    public virtual void Dispose() {
        provider?.Dispose();
        provider = null;
        GC.SuppressFinalize(this);
    }

    public void Compose(IServiceCollection services, string[] args, AppParts app) {
        var types = new AppTypes(ModuleProvider ?? new ScannedModules(app.Assembly));
        Core.Install(services, types);
        Install(services, types, args);
        app.Kernel.Install(services, types);
        foreach (var module in types.Modules) {
            module.Install(services);
        }
    }

    protected virtual void Install(IServiceCollection services, AppTypes types, string[] args) {
    }

    protected void Claim() {
        if (used) {
            throw new InvalidOperationException($"This {GetType().Name} was used before. Give a new host to each Start.");
        }
        used = true;
    }
}
