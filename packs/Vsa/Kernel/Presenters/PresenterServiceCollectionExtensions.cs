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
            // A forward to the presenter registration, so the handler is the presenter of the scope.
            foreach (var handler in HandlersOf(presenter)) {
                services.AddScoped(handler, provider => provider.GetRequiredService(presenter));
            }
        }
        return services;
    }

    static IEnumerable<Type> PresentersOf(AppTypes types) =>
        types.Modules.Select(module => module.GetType().Assembly).Distinct()
            .SelectMany(LoadedTypes.Of)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && t.IsSubclassOf(typeof(Presenter)));

    static IEnumerable<Type> HandlersOf(Type presenter) =>
        presenter.GetInterfaces()
            .Where(i => i.IsGenericType && !i.ContainsGenericParameters
                && i.GetGenericTypeDefinition() == typeof(IPresenterHandler<>));
}
