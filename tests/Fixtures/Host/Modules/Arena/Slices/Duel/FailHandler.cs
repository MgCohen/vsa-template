using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Arena.Slices.Duel;

sealed class FailHandler : CommandHandler<FailCommand, int> {
    public override Task<Result<int>> HandleAsync(Envelope<FailCommand> envelope) =>
        throw new InvalidOperationException("The arena failed.");
}
