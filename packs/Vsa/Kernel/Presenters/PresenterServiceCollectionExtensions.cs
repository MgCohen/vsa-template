namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Architecture;
using Umpire.Vsa.Contracts.Events;
using Umpire.Kernels;

public static class PresenterServiceCollectionExtensions {
    public static IServiceCollection AddPresenters(this IServiceCollection services, AppTypes types) {
        services.AddSingleton<LivePresenters>();
        services.AddSingleton<IIntegrationEventObserver>(
            provider => provider.GetRequiredService<LivePresenters>());
        services.AddScoped<PresenterScope>();
        foreach (var presenter in PresentersOf(types)) {
            services.AddScoped(presenter);
            foreach (var handler in HandlersOf(presenter)) {
                services.AddScoped(handler, ThePresenterOfTheScope(presenter));
            }
        }
        return services;
    }

    static Func<IServiceProvider, object> ThePresenterOfTheScope(Type presenter) =>
        provider => provider.GetRequiredService(presenter);

    static IEnumerable<Type> PresentersOf(AppTypes types) =>
        types.Modules.Select(module => module.GetType().Assembly).Distinct()
            .SelectMany(LoadedTypes.Of)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && t.IsSubclassOf(typeof(Presenter)));

    static IEnumerable<Type> HandlersOf(Type presenter) =>
        presenter.GetInterfaces()
            .Where(i => i.IsGenericType && !i.ContainsGenericParameters
                && i.GetGenericTypeDefinition() == typeof(IPresenterHandler<>));
}
