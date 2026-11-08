using Microsoft.Extensions.DependencyInjection;
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
            "The app has no host. Select one before Build: UseWebHost() or UseGenericHost().");
        var builder = selected.CreateBuilder(args);
        var types = new ModuleTypes(modules ?? VsaArchitecture.Modules());
        VsaArchitecture.InstallLayers(builder.Services, types);
        selected.Install(builder, types);
        foreach (var configure in services) {
            configure(builder.Services);
        }
        VsaArchitecture.ConfigureModules(builder, types);
        return selected.Build(builder);
    }
}
