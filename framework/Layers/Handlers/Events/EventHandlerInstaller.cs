using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Handlers.Events;

public sealed class EventHandlerInstaller : IVsaInstaller {
    public void Install(VsaContext context) {
        context.Services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        foreach (var (service, handler) in context.Implementations(typeof(IntegrationEventHandler<>))) {
            context.Services.Add(service, handler, VsaLifetime.Scoped);
        }
    }
}
