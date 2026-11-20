namespace Till.Slices.Put;

using Till.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class PutHandler<T> : CommandHandler<PutCommand<T>, int> where T : struct, IAmount {
    public override Task<Result<int>> HandleAsync(IEnvelope<PutCommand<T>> envelope) =>
        Task.FromResult<Result<int>>(envelope.Message.Amount);
}
