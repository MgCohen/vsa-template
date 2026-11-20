namespace Till.Slices.Swap;

using Till.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record SwapCommand<TFrom, TTo> : ICommand<string>
    where TFrom : struct, IAmount
    where TTo : struct, IAmount;
