using VSA.Contracts;
using VSA.Contracts.Events;

namespace VSA.Testing.Unit;

public sealed class RecordingEventPublisher : IIntegrationEventPublisher {
    readonly List<IntegrationEvent> published = [];

    public IReadOnlyList<IntegrationEvent> Published => published;

    public IEnumerable<TEvent> OfType<TEvent>() where TEvent : IntegrationEvent => published.OfType<TEvent>();

    public Task<Result<VSA.Contracts.Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct)
        where TEvent : IntegrationEvent {
        published.Add(integrationEvent);
        return Task.FromResult<Result<VSA.Contracts.Unit>>(VSA.Contracts.Unit.Value);
    }
}
