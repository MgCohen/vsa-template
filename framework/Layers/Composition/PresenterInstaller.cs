#if NET
using VSA.Contracts.Events;
using VSA.Module;
using VSA.Presenters;

namespace VSA.Composition;

public sealed class PresenterInstaller() : LayerInstaller("Presenters") {
    public override void Install(VsaContext context) {
        context.Services.AddSingleton<LivePresenters>();
        context.Services.AddSingleton<IIntegrationEventObserver>(resolver => resolver.Require<LivePresenters>());
        base.Install(context);
    }

    protected override void Install(VsaContext context, Type type) {
        if (!type.IsSubclassOf(typeof(Presenter))) {
            return;
        }
        if (!type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPresenterEvent<>))) {
            context.Services.Add(type, type, VsaLifetime.Scoped);
            return;
        }
        context.Services.Add(type, resolver => Connected(resolver, type), VsaLifetime.Scoped);
    }

    static Presenter Connected(IVsaResolver resolver, Type type) {
        var presenter = (Presenter)resolver.Create(type);
        resolver.Require<LivePresenters>().Connect(presenter, resolver.Resolve<LivePresenters.Dispatch>());
        return presenter;
    }
}
#endif
