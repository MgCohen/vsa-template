namespace Till.Slices.Pick;

using Till.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class PickHandler<T> : CommandHandler<PickCommand<T>, string> where T : struct, IAmount {
    public override Task<Result<string>> HandleAsync(IEnvelope<PickCommand<T>> envelope) =>
        Task.FromResult<Result<string>>(typeof(T).Name);
}
