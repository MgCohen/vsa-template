using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

// Calls each handler of the event in this process, in the scope of the publisher. Then it gives the event to each observer.
sealed class InProcessEventPublisher(IServiceProvider services) : IIntegrationEventPublisher {
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        foreach (var handler in services.GetServices<IntegrationEventHandler<TEvent>>()) {
            await handler.HandleAsync(integrationEvent, ct);
        }
        foreach (var observer in services.GetServices<IIntegrationEventObserver>()) {
            await observer.OnPublishedAsync(integrationEvent, ct);
        }
    }
}
