using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace VSA.Handlers.Requests;

static class RequestEndpointInstaller {
    internal static bool Install(IServiceCollection services, ModuleTypes types) {
        var handlers = types.Implementations(typeof(RoutedHandler<,>));
        if (handlers.Count == 0) {
            return false;
        }
        services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = handlers.Select(h => h.Implementation.Assembly).Distinct();
        });
        return true;
    }
}
