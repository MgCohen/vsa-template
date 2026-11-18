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
        foreach (var presenter in PresentersOf(types)) {
            if (ShowsEvents(presenter)) {
                services.AddScoped(presenter, provider => Connected(provider, presenter));
            } else {
                services.AddScoped(presenter);
            }
        }
        return services;
    }

    static IEnumerable<Type> PresentersOf(AppTypes types) =>
        types.Modules.Select(module => module.GetType().Assembly).Distinct()
            .SelectMany(LoadedTypes.Of)
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && t.IsSubclassOf(typeof(Presenter)));

    static bool ShowsEvents(Type presenter) =>
        presenter.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>));

    static Presenter Connected(IServiceProvider provider, Type presenter) {
        var instance = (Presenter)ActivatorUtilities.CreateInstance(provider, presenter);
        provider.GetRequiredService<LivePresenters>().Connect(instance, provider.GetService<LivePresenters.Dispatch>());
        return instance;
    }
}
