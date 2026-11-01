using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers.Events;
using VSA.Presenters;

namespace VSA.Host;

public static class VsaHostExtensions {
    // Finds the modules of the app (ModuleDiscovery), then registers them as AddVsa(modules) does.
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) =>
        builder.AddVsa([.. VSA.Module.ModuleDiscovery.CreateModules(VSA.Module.ModuleDiscovery.FindRoots())]);

    // Registers the defaults, then the handlers and presenters of each module, then calls Configure.
    // The host finds the projects of a module from the layers that its root recorded at build time.
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        var services = builder.Services;
        services.AddVsaBus(LoopbackAddress);
        services.AddVsaEvents();
        services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = [.. modules.Select(module => Load(module, "Handlers.Requests")).OfType<Assembly>()];
        });
        foreach (var module in modules) {
            foreach (var handler in Concrete(module, "Handlers.Events")) {
                services.AddScoped(BaseOf(handler, typeof(IntegrationEventHandler<>)), handler);
            }
            foreach (var presenter in Concrete(module, "Presenters").Where(t => t.IsSubclassOf(typeof(Presenter)))) {
                services.AddScoped(presenter);
            }
            module.Configure(new VSA.Module.ModuleBuilder(module.Name, services, builder.Configuration));
        }
        return builder;
    }

    // Maps one HTTP endpoint for each request handler.
    public static WebApplication MapVsaRequests(this WebApplication app) {
        app.UseFastEndpoints();
        return app;
    }

    // VSA.Sdk records the layer projects of the module root as AssemblyMetadata("VsaLayer", ...).
    // A layer that the module does not have gives null. A recorded layer that does not load is an error.
    static Assembly? Load(VSA.Module.Module module, string layer) {
        var name = $"{module.Name}.{layer}";
        var recorded = module.GetType().Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Any(a => a.Key == VSA.Module.ModuleMetadata.Layer && a.Value == name);
        return recorded ? Assembly.Load(name) : null;
    }

    static IEnumerable<Type> Concrete(VSA.Module.Module module, string layer) =>
        (Load(module, layer)?.GetTypes() ?? [])
            .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));

    static Type BaseOf(Type type, Type openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == openBase) {
                return b;
            }
        }
        throw new InvalidOperationException($"{type.FullName} must inherit {openBase.Name}.");
    }

    // The first address of this server, with a wildcard host replaced by localhost.
    static Uri LoopbackAddress(IServiceProvider provider) {
        var addresses = provider.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.Ordinal)) ?? addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("The server has no address for the bus.");
        foreach (var wildcard in new[] { "://+", "://*", "://[::]", "://0.0.0.0" }) {
            address = address.Replace(wildcard, "://localhost", StringComparison.Ordinal);
        }
        return new Uri(address);
    }
}
