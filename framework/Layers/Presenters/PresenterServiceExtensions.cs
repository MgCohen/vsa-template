using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;

namespace VSA.Presenters;

public static class PresenterServiceExtensions {
    // Registers LivePresenters as the observer of the events: each live presenter gets its presenter events.
    public static IServiceCollection AddVsaLivePresenters(this IServiceCollection services) {
        services.AddSingleton<LivePresenters>();
        services.AddSingleton<IIntegrationEventObserver>(provider => provider.GetRequiredService<LivePresenters>());
        return services;
    }

    // Registers a scoped presenter. A presenter that implements IPresenterEvent<TEvent> is added to LivePresenters
    // when the services make it, with the Dispatch of its scope if there is one. Its scope disposes it, which removes it.
    public static IServiceCollection AddVsaPresenter(this IServiceCollection services, Type presenter) {
        if (!presenter.IsSubclassOf(typeof(Presenter))) {
            throw new ArgumentException($"{presenter.FullName} must inherit Presenter.", nameof(presenter));
        }
        if (!presenter.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>))) {
            services.AddScoped(presenter);
            return services;
        }
        services.AddScoped(presenter, provider => {
            var instance = (Presenter)ActivatorUtilities.CreateInstance(provider, presenter);
            if (provider.GetService<LivePresenters>() is { } live) {
                instance.Connect(live.Add(instance, provider.GetService<LivePresenters.Dispatch>()));
            }
            return instance;
        });
        return services;
    }

    // Registers IBus as HttpBus. The base address is the server that maps the request handlers.
    public static IServiceCollection AddVsaBus(this IServiceCollection services, Func<IServiceProvider, Uri> baseAddress) {
        services.AddHttpClient<IBus, HttpBus>((provider, client) => client.BaseAddress = baseAddress(provider));
        return services;
    }
}
