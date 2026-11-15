namespace Cards.Slices.PayCost;

using Cards.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record PayCostCommand(Cost Cost) : ICommand<PayCostResult>;
