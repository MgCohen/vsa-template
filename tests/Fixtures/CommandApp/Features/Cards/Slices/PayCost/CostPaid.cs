using Cards.Domain.Shared;
using Umpire.Vsa.Contracts.Events;

namespace Cards.Slices.PayCost;

public sealed record CostPaid(Cost Cost) : IntegrationEvent;
