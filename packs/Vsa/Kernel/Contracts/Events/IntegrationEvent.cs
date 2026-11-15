namespace Umpire.Vsa.Contracts.Events;

using Umpire.Messages.Contracts;

public abstract record IntegrationEvent : IBroadcastMessage {
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
