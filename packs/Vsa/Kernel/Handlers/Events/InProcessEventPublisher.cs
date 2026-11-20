namespace Umpire.Vsa.Handlers.Events;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Messages.Handlers;

sealed class InProcessEventPublisher(IServiceProvider provider) : IIntegrationEventPublisher {
    public Task<Result<Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct)
        where TEvent : IntegrationEvent =>
        provider.GetRequiredService<Pipelines>().PublishAsync(integrationEvent, provider, ct);
}
