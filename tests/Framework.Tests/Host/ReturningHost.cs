using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Module;
using VSA.Kernel;

namespace Framework.Tests.Host;

sealed class ReturningHost : IAppHost {
    public IHostApplicationBuilder CreateBuilder(string[] args) => Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

    public void Install(IHostApplicationBuilder builder, AnchorTypes types) {
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
