namespace Umpire.Hosts.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using Umpire.Kernels;

public class WebHost : AppHost {
    WebApplication? web;
    AppSignals? signals;
    PosixSignalRegistration? terminate;
    CancellationTokenRegistration stopping;

    public override IServiceProvider Build(string[] args, App app) {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<IHostLifetime, AppLifetime>();
        var types = TypesOf(app);
        Compose(builder.Services, Arguments(args), app.Kernel, types);
        var root = Views.RootOf(types);
        if (root is not null) {
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        }
        web = builder.Build();
        web.Use(async (http, next) => {
            HostWork.MarkCurrentFlow();
            await next(http);
        });
        HttpApi.Map(web, Route);
        if (root is not null) {
            Views.Map(web, root);
        }
        return web.Services;
    }

    public override void Start(IServiceProvider services, AppStop stop) {
        web!.StartAsync().GetAwaiter().GetResult();
        stopping = web.Lifetime.ApplicationStopping.Register(() => stop.Request());
        signals = AppSignals.Listen(stop);
        terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => {
            context.Cancel = true;
            stop.Request();
        });
    }

    public override void Dispose() {
        signals?.Dispose();
        signals = null;
        terminate?.Dispose();
        terminate = null;
        stopping.Dispose();
        if (web is { } running) {
            web = null;
            running.StopAsync().GetAwaiter().GetResult();
            running.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        base.Dispose();
    }

    protected virtual string Route(Operation operation) => $"/api/{operation.Group}/{operation.Name}";
}
