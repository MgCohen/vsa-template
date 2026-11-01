using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

public static class EventServiceExtensions {
    // Registers IIntegrationEventPublisher as InProcessEventPublisher.
    public static IServiceCollection AddVsaEvents(this IServiceCollection services) {
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        return services;
    }
}
