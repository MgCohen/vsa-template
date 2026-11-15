using Umpire.Messages.Contracts;

namespace Umpire.Vsa.Contracts.Events;

public abstract record IntegrationEvent : IBroadcastMessage {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
