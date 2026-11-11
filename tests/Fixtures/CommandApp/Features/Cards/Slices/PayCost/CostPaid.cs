using Cards.Domain.Shared;
using VSA.Contracts.Events;

namespace Cards.Slices.PayCost;

public sealed record CostPaid(Cost Cost) : IntegrationEvent;
