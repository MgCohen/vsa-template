namespace Umpire.Vsa.Contracts.Events;

using Umpire.Messages.Contracts;

public interface IIntegrationEventPublisher {
    Task<Result<Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IntegrationEvent;
}
