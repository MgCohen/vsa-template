using VSA.Contracts;

namespace VSA.Contracts.Events;

public abstract record IntegrationEvent : IBroadcastMessage {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
