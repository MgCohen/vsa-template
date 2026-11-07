using VSA.Contracts.Events;
using VSA.Handlers.Events;
using VSA.Module;

namespace VSA.Composition;

sealed class InProcessEventPublisher(IVsaResolver resolver) : IIntegrationEventPublisher {
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent {
        foreach (var handler in resolver.ResolveAll<IntegrationEventHandler<TEvent>>()) {
            await handler.HandleAsync(integrationEvent, ct);
        }
        foreach (var observer in resolver.ResolveAll<IIntegrationEventObserver>()) {
            await observer.OnPublishedAsync(integrationEvent, ct);
        }
    }
}
