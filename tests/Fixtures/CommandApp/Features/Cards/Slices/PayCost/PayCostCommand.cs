using Cards.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

namespace Cards.Slices.PayCost;

public sealed record PayCostCommand(Cost Cost) : ICommand<PayCostResult>;
