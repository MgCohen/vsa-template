using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Handlers.Requests;
using VSA.Module;
using VSA.Views;

namespace VSA.Host;

sealed class WebVsaHost : IVsaHost {
    Type[] requestHandlers = [];
    Type? viewsRoot;

    public IHostApplicationBuilder CreateBuilder(string[] args) => WebApplication.CreateBuilder(args);

    public void Install(IHostApplicationBuilder builder, ModuleTypes types) {
        requestHandlers = [.. types.Implementations(typeof(RequestHandler<,>)).Select(handler => handler.Service)];
        viewsRoot = ViewsInstaller.FindRoot(AppScan.AssembliesThatReference(typeof(ViewsRoot).Assembly));
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
}
