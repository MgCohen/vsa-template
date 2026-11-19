namespace Changes.Slices.FindChanges;

using Changes.Domain.Shared;
using Changes.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FindChangesHandler : CommandHandler<FindChangesCommand, ChangesResult> {
    public override Task<Result<ChangesResult>> HandleAsync(Envelope<FindChangesCommand> envelope) {
        var command = envelope.Message;
        var hash = InputHash.Of(command.Block, command.Bin, command.Run);
        var same = StampFile.Read(command.Bin) == hash;
        return Task.FromResult<Result<ChangesResult>>(new ChangesResult([], same, hash));
    }
}
