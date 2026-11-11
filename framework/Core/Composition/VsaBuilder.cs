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

    VsaBuilder(string[] args) {
        this.args = args;
    }

    public static VsaBuilder Create(params string[] args) => new(args);

    public VsaBuilder UseHost(IVsaHost selected) {
        host = selected;
        return this;
    }

    public VsaBuilder UseGenericHost() => UseHost(new GenericVsaHost());

    public VsaBuilder Modules(params VSA.Module.Module[] given) {
        modules = given;
        return this;
    }

    public VsaBuilder Services(Action<IServiceCollection> configure) {
        services.Add(configure);
        return this;
    }

    public IHost Build() {
        var selected = host ?? throw new InvalidOperationException(
            "The app has no host. Select one before Build: UseWebHost() or UseGenericHost(). Or call services.AddVsa(args) with no host.");
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
        VsaArchitecture.InstallLayers(target, types);
        foreach (var configure in services) {
            configure(target);
        }
        VsaArchitecture.ConfigureModules(target, configuration, types);
    }

    ModuleTypes Types() => new(modules ?? VsaArchitecture.Modules());
}
