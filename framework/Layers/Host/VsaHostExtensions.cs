using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;
using VSA.Module;
using VSA.Presenters;

namespace VSA.Host;

public static class VsaHostExtensions {
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) =>
        builder.AddVsa([.. ModuleDiscovery.CreateModules(ModuleDiscovery.FindRoots())]);

    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        var services = builder.Services;
        services.AddVsaBus(LoopbackAddress);
        services.AddVsa(builder.Configuration, modules);
        AddRequestEndpoints(services, modules);
        return builder;
    }

    public static WebApplication MapVsaRequests(this WebApplication app) {
        if (app.Services.GetService<RequestEndpoints>() is not null) {
            app.UseFastEndpoints();
        }
        return app;
    }

    static void AddRequestEndpoints(IServiceCollection services, VSA.Module.Module[] modules) {
        Assembly[] requestHandlers = [.. modules
            .Select(module => ModuleRoot.Of(module).LayerAssembly("Handlers.Requests"))
            .OfType<Assembly>()];
        if (requestHandlers.Length == 0) {
            return;
        }
        services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = requestHandlers;
        });
        services.AddSingleton<RequestEndpoints>();
    }

    static Uri LoopbackAddress(IServiceProvider provider) {
        var addresses = provider.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()?.Addresses ?? [];
        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.Ordinal))
            ?? addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("The server has no address for the bus.");
        foreach (var wildcard in new[] { "://+", "://*", "://[::]", "://0.0.0.0" }) {
            address = address.Replace(wildcard, "://localhost", StringComparison.Ordinal);
        }
        return new Uri(address);
    }

    sealed class RequestEndpoints { }
}
