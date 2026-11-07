using System.Reflection;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace VSA.Handlers.Requests;

public sealed class RequestEndpointInstaller(IServiceCollection services) : IVsaInstaller {
    public void Install(VsaContext context) {
        Assembly[] requestHandlers = [.. context.Implementations(typeof(RoutedHandler<,>))
            .Select(handler => handler.Implementation.Assembly)
            .Distinct()];
        if (requestHandlers.Length == 0) {
            return;
        }
        services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = requestHandlers;
        });
        services.AddSingleton(this);
    }
}
