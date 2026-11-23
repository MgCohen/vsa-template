namespace Gauges.Slices.ReadGauge;

using Umpire.Vsa.Contracts.Events;

public sealed record GaugeRead(int Value) : IntegrationEvent;
