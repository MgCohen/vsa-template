namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

static class AutoRegistration {
    internal static void Add(IServiceCollection services, AppTypes types) {
        foreach (var type in types.Registered()) {
            AddClosed(services, type);
        }
    }

    internal static void AddClosed(IServiceCollection services, Type type) {
        var lifetime = MarkedTypes.LifetimeOf(type);
        services.TryAdd(new ServiceDescriptor(type, type, lifetime));
        foreach (var service in MarkedTypes.ServicesOf(type)) {
            services.Add(new ServiceDescriptor(service, new Forward(type).Resolve, lifetime));
        }
    }

    internal static Type? ImplementationOf(ServiceDescriptor descriptor) =>
        descriptor.IsKeyedService
            ? null
            : descriptor.ImplementationType ?? (descriptor.ImplementationFactory?.Target as Forward)?.Implementation;

    sealed class Forward(Type implementation) {
        public Type Implementation => implementation;

        public object Resolve(IServiceProvider provider) => provider.GetRequiredService(implementation);
    }
}
