namespace Arena.Slices.Duel;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FailHandler : CommandHandler<FailCommand, int> {
    public override Task<Result<int>> HandleAsync(IEnvelope<FailCommand> envelope) =>
        throw new InvalidOperationException("The arena failed.");
}
