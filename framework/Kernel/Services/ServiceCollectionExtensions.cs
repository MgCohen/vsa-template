namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection Replace<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService {
        var current = services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException($"{typeof(TService).Name} has no default registration to replace.");
        services.RemoveAll<TService>();
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), current.Lifetime));
        return services;
    }
}
