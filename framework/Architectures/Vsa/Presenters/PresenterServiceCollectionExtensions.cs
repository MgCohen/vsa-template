namespace VSA.Presenters;

using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Kernel;

public static class PresenterServiceCollectionExtensions {
    public static IServiceCollection AddPresenters(this IServiceCollection services, AppTypes types) {
        services.AddSingleton<LivePresenters>();
        services.AddSingleton<IIntegrationEventObserver>(
            provider => provider.GetRequiredService<LivePresenters>());
        var presenters = types.Implementations(typeof(Presenter)).Select(p => p.Implementation).Where(ShowsEvents);
        foreach (var presenter in presenters) {
            services.AddScoped(presenter, provider => Connected(provider, presenter));
        }
        return services;
    }

    static bool ShowsEvents(Type presenter) =>
        presenter.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>));

    static Presenter Connected(IServiceProvider provider, Type presenter) {
        var instance = (Presenter)ActivatorUtilities.CreateInstance(provider, presenter);
        var dispatch = provider.GetService<LivePresenters.Dispatch>();
        provider.GetRequiredService<LivePresenters>().Connect(instance, dispatch);
        return instance;
    }
}
