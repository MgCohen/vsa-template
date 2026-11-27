namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Config;

public class AppHost : IDisposable {
    ServiceProvider? provider;

    public AppHost() : this([]) {
    }

    public AppHost(string[] args) {
        Args = args ?? throw new ArgumentNullException(nameof(args));
    }

    public IModuleProvider ModuleProvider { get; init; } = new ScannedModules();

    protected string[] Args { get; set; }

    public virtual IServiceProvider Build(App app) {
        var services = new ServiceCollection();
        Compose(services, Arguments(Args), app);
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

    public void Compose(IServiceCollection services, HostArguments arguments, Kernel kernel, AppTypes types) {
        new Core(arguments).Install(services, types);
        kernel.Install(services, types);
        var kernelServices = OpenDescriptors.Snapshot(services);
        var settings = new ModuleSettings(Settings(types));
        foreach (var module in types.Modules) {
            module.Install(services, settings);
        }
        CloseOpenDescriptors(services, types, kernelServices);
        Configure(services, types);
        DefaultLogging.AddIfMissing(services);
    }

    protected void Compose(IServiceCollection services, HostArguments arguments, App app) =>
        Compose(services, arguments, app.Kernel, TypesOf(app));

    protected virtual HostArguments Arguments(string[] args) => HostArguments.Read(args);

    protected virtual AppTypes TypesOf(App app) => new(ModuleProvider, app.GetType().Assembly);

    protected virtual IReadOnlyList<ISettingsProvider> Settings(AppTypes types) =>
        [new UmpireConfigSettings(UmpireConfig.In(AppContext.BaseDirectory))];

    protected virtual void Configure(IServiceCollection services, AppTypes types) {
    }

    static void CloseOpenDescriptors(
        IServiceCollection services, AppTypes types, HashSet<ServiceDescriptor> kernelServices) =>
        OpenDescriptors.Close(services, types, kernelServices);
}
