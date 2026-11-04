using VSA.Contracts.Events;

namespace Pipeline.Slices.Messages;

// A Shared event. The event handlers of each module get it.
public sealed record MessageHandled(long MessageId) : IntegrationEvent;
