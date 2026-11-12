using VSA.Contracts.Events;

namespace Signals.Slices.Ring;

public sealed record Rang(string Bell) : IntegrationEvent;
