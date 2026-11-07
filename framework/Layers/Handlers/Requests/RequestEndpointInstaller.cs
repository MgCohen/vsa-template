using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace VSA.Handlers.Requests;

public static class RequestEndpointInstaller {
    public static void Install(VsaContext context) {
        Assembly[] requestHandlers = [.. context.Implementations(typeof(RoutedHandler<,>))
            .Select(handler => handler.Implementation.Assembly)
            .Distinct()];
        if (requestHandlers.Length == 0) {
            return;
        }
        context.Services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = requestHandlers;
        });
        context.Services.AddSingleton(new InstalledRequestEndpoints());
    }

    public static void Map(IApplicationBuilder app) {
        if (app.ApplicationServices.GetService<InstalledRequestEndpoints>() is not null) {
            app.UseFastEndpoints();
        }
    }

    sealed class InstalledRequestEndpoints;
}
