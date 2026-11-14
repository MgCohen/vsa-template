using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using VSA.Module;

namespace VSA.Composition;

public sealed class VsaBuilder {
    readonly string[] args;
    readonly List<Action<IServiceCollection>> services = [];
    IVsaHost? host;
    IReadOnlyList<VSA.Module.Module>? modules;
    IKeySource keys = new ConstraintKeys();

    VsaBuilder(string[] args) {
        this.args = args;
    }

    public static VsaBuilder Create(params string[] args) => new(args);

    public VsaBuilder UseHost(IVsaHost selected) {
        host = selected;
        return this;
    }

    public VsaBuilder UseGenericHost() => UseHost(new GenericVsaHost());

    public VsaBuilder UseConsoleHost() => UseHost(new ConsoleVsaHost());

    public VsaBuilder Modules(params VSA.Module.Module[] given) {
        modules = given;
        return this;
    }

    public VsaBuilder Keys(IKeySource given) {
        keys = given ?? throw new ArgumentNullException(nameof(given));
        return this;
    }

    public VsaBuilder Services(Action<IServiceCollection> configure) {
        services.Add(configure);
        return this;
    }

    public IVsaHost SelectedHost() => host ??= new GenericVsaHost();

    public IHost Build() {
        var selected = SelectedHost();
        var builder = selected.CreateBuilder(args);
        var types = Types();
        selected.Install(builder, types);
        Compose(builder.Services, builder.Configuration, types);
        return selected.Build(builder);
    }

    internal IServiceCollection Compose(IServiceCollection services) {
        Compose(services, ConfigurationOf(services) ?? new ConfigurationBuilder().AddCommandLine(args).Build(), Types());
        return services;
    }

    static IConfiguration? ConfigurationOf(IServiceCollection services) =>
        services.LastOrDefault(service => service.ServiceType == typeof(IConfiguration))?.ImplementationInstance as IConfiguration;

    void Compose(IServiceCollection target, IConfiguration configuration, ModuleTypes types) {
        target.TryAddSingleton(configuration);
        target.AddLogging();
        target.AddSingleton(types);
        VsaArchitecture.InstallLayers(target, types);
        foreach (var configure in services) {
            configure(target);
        }
        VsaArchitecture.ConfigureModules(target, configuration, types);
    }

    ModuleTypes Types() => new(modules ?? VsaArchitecture.Modules(), keys);
}
