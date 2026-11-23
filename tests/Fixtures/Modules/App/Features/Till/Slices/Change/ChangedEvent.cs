namespace Till.Slices.Change;

using Till.Domain.Shared;
using Umpire.Vsa.Contracts.Events;

public sealed record ChangedEvent<T>(int Amount) : IntegrationEvent where T : struct, IAmount;
