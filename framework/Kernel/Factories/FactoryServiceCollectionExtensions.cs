namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;

public static class FactoryServiceCollectionExtensions {
    public static IServiceCollection AddFactory<T, TService>(this IServiceCollection services, Func<TService, T> create)
        where TService : notnull =>
        services.AddTransient<IFactory<T>>(provider =>
            new FuncFactory<T>(() => create(provider.GetRequiredService<TService>())));

    sealed class FuncFactory<T>(Func<T> create) : IFactory<T> {
        public T Create() => create();
    }
}
