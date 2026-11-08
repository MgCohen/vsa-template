using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Handlers.Events;

public static class EventHandlerInstaller {
    public static void Install(VsaContext context) {
        var handlers = context.Implementations(typeof(IntegrationEventHandler<>));
        context.Services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        foreach (var (service, handler) in handlers) {
            context.Services.AddScoped(service, handler);
        }
    }
}
