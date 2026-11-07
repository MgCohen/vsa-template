using System.Reflection;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;
using VSA.Module;

namespace VSA.Host;

public sealed class RequestEndpointInstaller(IServiceCollection services) : IVsaInstaller {
    public void Install(VsaContext context) {
        Assembly[] requestHandlers = [.. context.Modules
            .Select(module => ModuleRoot.Of(module).LayerAssembly("Handlers.Requests"))
            .OfType<Assembly>()];
        if (requestHandlers.Length == 0) {
            return;
        }
        services.AddFastEndpoints(options => {
            options.DisableAutoDiscovery = true;
            options.Assemblies = requestHandlers;
        });
        services.AddSingleton<RequestEndpoints>();
    }
}
