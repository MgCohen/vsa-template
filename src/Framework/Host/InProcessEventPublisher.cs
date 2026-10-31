using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Handlers.Events;

namespace VSA.Host;

// Calls each handler of the event in this process, in the scope of the publisher.
sealed class InProcessEventPublisher(IServiceProvider services) : IIntegrationEventPublisher {
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        foreach (var handler in services.GetServices<IntegrationEventHandler<TEvent>>()) {
            await handler.HandleAsync(integrationEvent, ct);
        }
    }
}
