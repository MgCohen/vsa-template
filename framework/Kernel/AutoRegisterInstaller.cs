using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VSA.Kernel;

sealed class AutoRegisterInstaller : IInstaller {
    public void Install(IServiceCollection services, AnchorTypes types) {
        foreach (var type in types.Registered()) {
            var lifetime = MarkedTypes.LifetimeOf(type);
            services.TryAdd(new ServiceDescriptor(type, type, lifetime));
            foreach (var service in MarkedTypes.ServicesOf(type)) {
                services.Add(new ServiceDescriptor(service, provider => provider.GetRequiredService(type), lifetime));
            }
        }
    }
}
