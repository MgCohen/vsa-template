using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Module;

namespace Framework.Tests.Host;

sealed class ReturningHost : IVsaHost {
    public IHostApplicationBuilder CreateBuilder(string[] args) => Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
    }

    public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

    public int Run(IHost app) {
        app.StartAsync().GetAwaiter().GetResult();
        return 0;
    }

    public void Stop(IHost app) {
        app.StopAsync().GetAwaiter().GetResult();
        app.Dispose();
    }
}
