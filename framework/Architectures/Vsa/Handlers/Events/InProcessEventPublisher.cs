using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Contracts.Events;

namespace VSA.Handlers.Events;

sealed class InProcessEventPublisher(IServiceProvider provider) : IIntegrationEventPublisher {
    public async Task<Result<Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct)
        where TEvent : IntegrationEvent {
        var result = await provider.GetRequiredService<Pipelines>()
            .SendAsync<Unit>(PipelineKind.Events, integrationEvent, provider, ct);
        if (result.Envelope?.State == EnvelopeState.Accepted) {
            foreach (var observer in provider.GetServices<IIntegrationEventObserver>()) {
                await observer.OnPublishedAsync(integrationEvent, ct);
            }
        }
        return result;
    }
}
