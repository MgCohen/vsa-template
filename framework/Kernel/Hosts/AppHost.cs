namespace Umpire.Kernels;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Config;

public class AppHost : IDisposable {
    ServiceProvider? provider;

    public IReadOnlyList<Module>? Modules { get; init; }

    public virtual IServiceProvider Build(string[] args, App app) {
        var services = new ServiceCollection();
        Compose(services, Configuration(args), app);
        provider = services.BuildServiceProvider();
        return provider;
    }

    public virtual void Start(IServiceProvider services, AppStop stop) {
    }

    public virtual void Dispose() {
        provider?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        provider = null;
        GC.SuppressFinalize(this);
    }

    public void Compose(IServiceCollection services, IConfiguration configuration, Kernel kernel, AppTypes types) {
        kernel.Install(services, types, configuration);
        var kernelServices = OpenDescriptors.Snapshot(services);
        var settings = new ModuleSettings(Settings(types));
        foreach (var module in types.Modules) {
            module.Compose(services, settings);
        }
        CloseOpenDescriptors(services, types, kernelServices);
        Configure(services, types);
    }

    protected void Compose(IServiceCollection services, IConfiguration configuration, App app) =>
        Compose(services, configuration, app.Pack.Kernel, TypesOf(app));

    protected virtual IConfiguration Configuration(string[] args) =>
        new ConfigurationBuilder().AddCommandLine(HostArguments.Options(args)).Build();

    protected AppTypes TypesOf(App app) => new(Modules, app.GetType().Assembly);

    protected virtual IReadOnlyList<ISettingsProvider> Settings(AppTypes types) =>
        [new UmpireConfigSettings(UmpireConfig.In(AppContext.BaseDirectory))];

    protected virtual void Configure(IServiceCollection services, AppTypes types) {
    }

    static void CloseOpenDescriptors(
        IServiceCollection services, AppTypes types, HashSet<ServiceDescriptor> kernelServices) =>
        OpenDescriptors.Close(services, types, kernelServices);
}
