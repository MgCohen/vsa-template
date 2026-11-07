using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Presenters;

public static class PresenterInstaller {
    public static void Install(VsaContext context) {
        context.Services.AddSingleton<LivePresenters>();
        context.Services.AddSingleton<IIntegrationEventObserver>(
            provider => provider.GetRequiredService<LivePresenters>());
        foreach (var (_, presenter) in context.Implementations(typeof(Presenter))) {
            if (ShowsEvents(presenter)) {
                context.Services.AddScoped(presenter, provider => Connected(provider, presenter));
            } else {
                context.Services.AddScoped(presenter);
            }
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
