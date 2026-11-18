namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

static class AutoRegistration {
    internal static void Add(IServiceCollection services, AppTypes types) {
        foreach (var type in types.Registered()) {
            var lifetime = MarkedTypes.LifetimeOf(type);
            services.TryAdd(new ServiceDescriptor(type, type, lifetime));
            foreach (var service in MarkedTypes.ServicesOf(type)) {
                services.Add(new ServiceDescriptor(service, provider => provider.GetRequiredService(type), lifetime));
            }
        }
    }
}
