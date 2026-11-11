using Microsoft.Extensions.DependencyInjection;

namespace VSA.Composition;

public static class FactoryServiceCollectionExtensions {
    public static IServiceCollection AddFactory<TFactory>(
        this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) where TFactory : class {
        var factoryInterfaces = typeof(TFactory).GetInterfaces().Where(IsFactory).ToArray();
        if (factoryInterfaces.Length == 0) {
            throw new ArgumentException(
                $"{typeof(TFactory).FullName} implements no factory interface. Implement IFactory<T> or IFactory<TArg, T>.",
                nameof(TFactory));
        }
        services.Add(new ServiceDescriptor(typeof(TFactory), typeof(TFactory), lifetime));
        foreach (var factoryInterface in factoryInterfaces) {
            services.Add(new ServiceDescriptor(factoryInterface, provider => provider.GetRequiredService<TFactory>(), lifetime));
        }
        return services;
    }

    public static IServiceCollection AddFactory<T, TService>(this IServiceCollection services, Func<TService, T> create)
        where TService : notnull =>
        services.AddTransient<IFactory<T>>(provider =>
            new FuncFactory<T>(() => create(provider.GetRequiredService<TService>())));

    public static IServiceCollection AddFactory<T, TService1, TService2>(
        this IServiceCollection services, Func<TService1, TService2, T> create)
        where TService1 : notnull where TService2 : notnull =>
        services.AddTransient<IFactory<T>>(provider =>
            new FuncFactory<T>(() => create(provider.GetRequiredService<TService1>(), provider.GetRequiredService<TService2>())));

    static bool IsFactory(Type type) =>
        type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(IFactory<>) || type.GetGenericTypeDefinition() == typeof(IFactory<,>));

    sealed class FuncFactory<T>(Func<T> create) : IFactory<T> {
        public T Create() => create();
    }
}
