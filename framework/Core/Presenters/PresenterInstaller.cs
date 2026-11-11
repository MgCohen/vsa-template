using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Presenters;

static class PresenterInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        var presenters = types.Implementations(typeof(Presenter));
        services.AddSingleton<LivePresenters>();
        services.AddSingleton<IIntegrationEventObserver>(
            provider => provider.GetRequiredService<LivePresenters>());
        foreach (var (_, presenter) in presenters.Where(p => ShowsEvents(p.Implementation))) {
            services.AddScoped(presenter, provider => Connected(provider, presenter));
        }
    }

    static bool ShowsEvents(Type presenter) =>
        presenter.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>));

    static Presenter Connected(IServiceProvider provider, Type presenter) {
        var instance = (Presenter)ActivatorUtilities.CreateInstance(provider, presenter);
        var dispatch = provider.GetService<LivePresenters.Dispatch>();
        provider.GetRequiredService<LivePresenters>().Connect(instance, dispatch);
        return instance;
    }
}
