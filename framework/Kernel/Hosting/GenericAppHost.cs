using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VSA.Kernel;

public sealed class GenericAppHost : IAppHost {
    public IHostApplicationBuilder CreateBuilder(string[] args) => Host.CreateApplicationBuilder(args);

    public void Install(IHostApplicationBuilder builder, AnchorTypes types) {
    }

    public IHost Build(IHostApplicationBuilder builder) => ((HostApplicationBuilder)builder).Build();

    public int Run(IHost app) {
        app.Run();
        return 0;
    }

    public void Stop(IHost app) => app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
}
