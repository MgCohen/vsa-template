namespace Unbound.Slices.Mark;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class MarkHandler<T, TLabel> : CommandHandler<MarkCommand<T>, int> {
    public override Task<Result<int>> HandleAsync(IEnvelope<MarkCommand<T>> envelope) =>
        Task.FromResult<Result<int>>(1);
}
