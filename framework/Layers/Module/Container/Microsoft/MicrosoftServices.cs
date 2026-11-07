using Microsoft.Extensions.DependencyInjection;

namespace VSA.Module;

public sealed class MicrosoftServices : IVsaServices {
    readonly IServiceCollection services;

    public MicrosoftServices(IServiceCollection services) {
        this.services = services;
        if (!services.Any(s => s.ServiceType == typeof(IVsaResolver))) {
            services.AddTransient<IVsaResolver>(provider => new MicrosoftResolver(provider));
        }
    }

    public void Add(Type service, Type implementation, VsaLifetime lifetime) =>
        services.Add(new ServiceDescriptor(service, implementation, LifetimeOf(lifetime)));

    public void Add(Type service, Func<IVsaResolver, object> factory, VsaLifetime lifetime) =>
        services.Add(new ServiceDescriptor(service, provider => factory(new MicrosoftResolver(provider)), LifetimeOf(lifetime)));

    public void AddInstance(Type service, object instance) =>
        services.Add(new ServiceDescriptor(service, instance));

    public VsaLifetime? LifetimeOf(Type service) =>
        services.LastOrDefault(s => s.ServiceType == service) is { } last ? LifetimeOf(last.Lifetime) : null;

    public void RemoveAll(Type service) {
        for (var i = services.Count - 1; i >= 0; i--) {
            if (services[i].ServiceType == service) {
                services.RemoveAt(i);
            }
        }
    }

    static ServiceLifetime LifetimeOf(VsaLifetime lifetime) => lifetime switch {
        VsaLifetime.Singleton => ServiceLifetime.Singleton,
        VsaLifetime.Scoped => ServiceLifetime.Scoped,
        _ => ServiceLifetime.Transient,
    };

    static VsaLifetime LifetimeOf(ServiceLifetime lifetime) => lifetime switch {
        ServiceLifetime.Singleton => VsaLifetime.Singleton,
        ServiceLifetime.Scoped => VsaLifetime.Scoped,
        _ => VsaLifetime.Transient,
    };
}
