using Cards.Domain.Shared;
using VSA.Contracts.Commands;

namespace Cards.Slices.PayCost;

public sealed record PayCostCommand(Cost Cost) : ICommand<PayCostResult>;
