namespace VSA.Host;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Kernel;
using VSA.Views;

public sealed class WebVsaHost : VsaHost {
    Type[] requestHandlers = [];
    Type? viewsRoot;

    protected override IHostApplicationBuilder CreateBuilder(string[] args) => WebApplication.CreateBuilder(args);

    protected override void Install(IServiceCollection services, AppTypes types) {
        base.Install(services, types);
        requestHandlers = [.. types.Implementations(typeof(RequestHandler<,>)).Select(handler => handler.Service)];
        viewsRoot = ViewsInstaller.FindRoot(AppTypes.AssembliesThatReference(typeof(ViewsRoot).Assembly));
        if (viewsRoot is not null) {
            ViewsInstaller.Install(services);
        }
    }

    protected override IHost Build(IHostApplicationBuilder builder) {
        var app = ((WebApplicationBuilder)builder).Build();
        HttpApi.Map(app, requestHandlers);
        if (viewsRoot is not null) {
            ViewsInstaller.Map(app, viewsRoot);
        }
        return app;
    }
}
