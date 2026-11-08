using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Host;

namespace Framework.Tests.Host;

static class WebApps {
    public static VsaBuilder OnTestServer(params VSA.Module.Module[] modules) =>
        VsaBuilder.Create()
            .UseWebHost()
            .Modules(modules)
            .Services(services => services.AddSingleton<IServer, TestServer>());

    public static async Task<IHost> Start(this VsaBuilder builder) {
        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    public static IServiceCollection Registrations(this VsaBuilder builder) {
        IServiceCollection? registrations = null;
        builder.Services(services => registrations = services).Build().Dispose();
        return registrations!;
    }
}
