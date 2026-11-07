using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

sealed class InProcessEventPublisher(IServiceProvider provider) : IIntegrationEventPublisher {
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        foreach (var handler in provider.GetServices<IntegrationEventHandler<TEvent>>()) {
            await handler.HandleAsync(integrationEvent, ct);
        }
        foreach (var observer in provider.GetServices<IIntegrationEventObserver>()) {
            await observer.OnPublishedAsync(integrationEvent, ct);
        }
    }
}
