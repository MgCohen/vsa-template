using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;

namespace Umpire.Vsa.Testing.Unit;

public sealed class RecordingEventPublisher : IIntegrationEventPublisher {
    readonly List<IntegrationEvent> published = [];

    public IReadOnlyList<IntegrationEvent> Published => published;

    public IEnumerable<TEvent> OfType<TEvent>() where TEvent : IntegrationEvent => published.OfType<TEvent>();

    public Task<Result<Umpire.Messages.Contracts.Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct)
        where TEvent : IntegrationEvent {
        published.Add(integrationEvent);
        return Task.FromResult<Result<Umpire.Messages.Contracts.Unit>>(Umpire.Messages.Contracts.Unit.Value);
    }
}
