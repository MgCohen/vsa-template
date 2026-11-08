using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace VSA.Handlers.Requests;

public static class RequestEndpointInstaller {
    public static void Install(VsaContext context) {
        var handlers = context.Implementations(typeof(RoutedHandler<,>));
        if (handlers.Count == 0) {
            return;
        }
        context.Services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = handlers.Select(h => h.Implementation.Assembly).Distinct();
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
