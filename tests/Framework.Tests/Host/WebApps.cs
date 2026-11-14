using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Host;
using VSA.Kernel;

namespace Framework.Tests.Host;

static class WebApps {
    public static AppBuilder OnTestServer(params VSA.Module.Module[] modules) =>
        AppBuilder.Create()
            .UseWebHost()
            .Modules(modules)
            .Services(services => services.AddSingleton<IServer, TestServer>());

    public static async Task<IHost> Start(this AppBuilder builder) {
        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    public static IServiceCollection Registrations(this AppBuilder builder) {
        IServiceCollection? registrations = null;
        builder.Services(services => registrations = services).Build().Dispose();
        return registrations!;
    }
}
