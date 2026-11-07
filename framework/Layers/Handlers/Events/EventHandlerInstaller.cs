using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Handlers.Events;

public sealed class EventHandlerInstaller() : LayerInstaller("Handlers.Events") {
    public override void Install(VsaContext context) {
        context.Services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        base.Install(context);
    }

    protected override void Install(VsaContext context, Type type) {
        if (ClosedBase(type, typeof(IntegrationEventHandler<>)) is { } handledEvent) {
            context.Services.Add(handledEvent, type, VsaLifetime.Scoped);
        }
    }
}
