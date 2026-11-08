using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;

namespace VSA.Host;

public static class VsaHostExtensions {
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) =>
        builder.AddVsa(VsaComposer.DiscoveredModules());

    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        var composer = new VsaComposer(new MicrosoftServices(builder.Services), builder.Configuration, modules);
        composer.InstallLayers();
        AddLoopbackBus(builder.Services);
        new RequestEndpointInstaller(builder.Services).Install(composer.Context);
        composer.ConfigureModules();
        return builder;
    }

    public static WebApplication MapVsaRequests(this WebApplication app) {
        if (app.Services.GetService<RequestEndpointInstaller>() is not null) {
            app.UseFastEndpoints();
        }
        return app;
    }

    public static IServiceCollection AddLoopbackBus(IServiceCollection services) =>
        services.AddHttpClient<IBus, HttpBus>((provider, client) => client.BaseAddress = LoopbackAddress(provider)).Services;

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
}
