using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Presenters;
using VSA.Views;

namespace VSA.Host;

sealed class WebVsaHost : IVsaHost {
    bool requestEndpoints;
    Type? viewsRoot;

    public IHostApplicationBuilder CreateBuilder(string[] args) => WebApplication.CreateBuilder(args);

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
        builder.Services.AddHttpClient<IBus, HttpBus>(
            (provider, client) => client.BaseAddress = LoopbackAddress(provider));
        requestEndpoints = RequestEndpointInstaller.Install(builder.Services, types);
        viewsRoot = ViewsInstaller.FindRoot(AppScan.AssembliesThatReference(typeof(ViewsRoot).Assembly));
        if (viewsRoot is not null) {
            ViewsInstaller.Install(builder.Services);
        }
    }

    public IHost Build(IHostApplicationBuilder builder) {
        var app = ((WebApplicationBuilder)builder).Build();
        if (requestEndpoints) {
            app.UseFastEndpoints();
        }
        if (viewsRoot is not null) {
            ViewsInstaller.Map(app, viewsRoot);
        }
        return app;
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
