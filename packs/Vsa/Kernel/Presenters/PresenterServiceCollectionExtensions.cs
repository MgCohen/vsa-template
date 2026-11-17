namespace Umpire.Vsa.Presenters;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Vsa.Contracts.Events;
using Umpire.Kernel;

public static class PresenterServiceCollectionExtensions {
    public static IServiceCollection AddPresenters(this IServiceCollection services, AppTypes types) {
        services.AddSingleton<LivePresenters>();
        services.AddSingleton<IIntegrationEventObserver>(
            provider => provider.GetRequiredService<LivePresenters>());
        foreach (var presenter in types.ConcreteTypesOf(typeof(Presenter))) {
            if (ShowsEvents(presenter)) {
                services.AddScoped(presenter, provider => Connected(provider, presenter));
            } else {
                services.AddScoped(presenter);
            }
        }
        return services;
    }

    static bool ShowsEvents(Type presenter) =>
        presenter.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>));

    static Presenter Connected(IServiceProvider provider, Type presenter) {
        var instance = (Presenter)ActivatorUtilities.CreateInstance(provider, presenter);
        provider.GetRequiredService<LivePresenters>().Connect(instance, provider.GetService<LivePresenters.Dispatch>());
        return instance;
    }
}
