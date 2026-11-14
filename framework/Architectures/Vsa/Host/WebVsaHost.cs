using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Kernel;
using VSA.Views;

namespace VSA.Host;

public sealed class WebVsaHost : IAppHost {
    Type[] requestHandlers = [];
    Type? viewsRoot;

    public IHostApplicationBuilder CreateBuilder(string[] args) => WebApplication.CreateBuilder(args);

    public void Install(IHostApplicationBuilder builder, AnchorTypes types) {
        requestHandlers = [.. types.Implementations(typeof(RequestHandler<,>)).Select(handler => handler.Service)];
        viewsRoot = ViewsInstaller.FindRoot(AnchorScan.AssembliesThatReference(typeof(ViewsRoot).Assembly));
        if (viewsRoot is not null) {
            ViewsInstaller.Install(builder.Services);
        }
    }

    public IHost Build(IHostApplicationBuilder builder) {
        var app = ((WebApplicationBuilder)builder).Build();
        HttpApi.Map(app, requestHandlers);
        if (viewsRoot is not null) {
            ViewsInstaller.Map(app, viewsRoot);
        }
        return app;
    }

    public int Run(IHost app) {
        app.Run();
        return 0;
    }

    public void Stop(IHost app) => app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
}
