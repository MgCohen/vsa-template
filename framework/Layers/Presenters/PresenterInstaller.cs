using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Presenters;

public sealed class PresenterInstaller : IVsaInstaller {
    public void Install(VsaContext context) {
        context.Services.AddSingleton<LivePresenters>();
        context.Services.AddSingleton<IIntegrationEventObserver>(resolver => resolver.Require<LivePresenters>());
        foreach (var (_, presenter) in context.Implementations(typeof(Presenter))) {
            if (ShowsEvents(presenter)) {
                context.Services.Add(presenter, resolver => Connected(resolver, presenter), VsaLifetime.Scoped);
            } else {
                context.Services.Add(presenter, presenter, VsaLifetime.Scoped);
            }
        }
    }

    static bool ShowsEvents(Type presenter) =>
        presenter.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>));

    static Presenter Connected(IVsaResolver resolver, Type presenter) {
        var instance = (Presenter)resolver.Create(presenter);
        resolver.Require<LivePresenters>().Connect(instance, resolver.Resolve<LivePresenters.Dispatch>());
        return instance;
    }
}
