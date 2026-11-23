namespace Till.Slices.Swap;

using Till.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class SwapHandler<TFrom, TTo> : CommandHandler<SwapCommand<TFrom, TTo>, string>
    where TFrom : struct, IAmount
    where TTo : struct, IAmount {
    public override Task<Result<string>> HandleAsync(IEnvelope<SwapCommand<TFrom, TTo>> envelope) =>
        Task.FromResult<Result<string>>($"{typeof(TFrom).Name}->{typeof(TTo).Name}");
}
