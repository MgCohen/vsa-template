namespace Signals.Slices.Ring;

using Umpire.Vsa.Contracts.Events;

public sealed record Rang(string Bell) : IntegrationEvent;
