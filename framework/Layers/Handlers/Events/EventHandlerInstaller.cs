using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Module;

namespace VSA.Handlers.Events;

static class EventHandlerInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        var handlers = types.Implementations(typeof(IntegrationEventHandler<>));
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        foreach (var (service, handler) in handlers) {
            services.AddScoped(service, handler);
        }
    }
}
