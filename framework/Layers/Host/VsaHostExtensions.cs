using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.Host;

public static class VsaHostExtensions {
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) =>
        builder.AddVsa(new VsaComposer(builder.Services, builder.Configuration));

    public static WebApplicationBuilder AddVsa(
        this WebApplicationBuilder builder, params VSA.Module.Module[] modules) =>
        builder.AddVsa(new VsaComposer(builder.Services, builder.Configuration, modules));

    public static WebApplication MapVsaRequests(this WebApplication app) {
        RequestEndpointInstaller.Map(app);
        return app;
    }

    public static IServiceCollection AddLoopbackBus(IServiceCollection services) =>
        services.AddHttpClient<IBus, HttpBus>((provider, client) => client.BaseAddress = LoopbackAddress(provider)).Services;

    static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, VsaComposer composer) {
        AddLoopbackBus(builder.Services);
        RequestEndpointInstaller.Install(composer.Context);
        composer.Install();
        return builder;
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
}
