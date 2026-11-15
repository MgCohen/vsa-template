namespace Umpire.Vsa.Handlers.Events;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Messages.Handlers;

sealed class InProcessEventPublisher(IServiceProvider provider) : IIntegrationEventPublisher {
    public async Task<Result<Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct)
        where TEvent : IntegrationEvent {
        var result = await provider.GetRequiredService<Pipelines>()
            .SendAsync<Unit>(typeof(IntegrationEventHandler<>), integrationEvent, provider, ct);
        if (result.Envelope?.State == EnvelopeState.Accepted) {
            foreach (var observer in provider.GetServices<IIntegrationEventObserver>()) {
                await observer.OnPublishedAsync(integrationEvent, ct);
            }
        }
        return result;
    }
}
