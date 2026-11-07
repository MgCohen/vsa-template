using VSA.Handlers.Events;
using VSA.Module;

namespace VSA.Composition;

public sealed class EventHandlerInstaller() : LayerInstaller("Handlers.Events") {
    protected override void Install(VsaContext context, Type type) {
        if (ClosedBase(type, typeof(IntegrationEventHandler<>)) is { } handledEvent) {
            context.Services.Add(handledEvent, type, VsaLifetime.Scoped);
        }
    }
}
