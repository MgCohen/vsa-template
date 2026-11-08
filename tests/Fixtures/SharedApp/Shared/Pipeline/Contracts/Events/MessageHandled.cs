using VSA.Contracts.Events;

namespace Pipeline.Contracts.Events;

// A Shared event. The event handlers of each module get it.
public sealed record MessageHandled(long MessageId) : IntegrationEvent;
