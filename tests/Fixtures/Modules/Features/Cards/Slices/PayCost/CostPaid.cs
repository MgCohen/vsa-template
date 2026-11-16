namespace Cards.Slices.PayCost;

using Cards.Domain.Shared;
using Umpire.Vsa.Contracts.Events;

public sealed record CostPaid(Cost Cost) : IntegrationEvent;
