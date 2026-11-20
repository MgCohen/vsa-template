namespace Changes.Slices.FindChanges;

using Changes.Domain.Shared;
using Changes.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class FindChangesHandler : CommandHandler<FindChangesCommand, ChangesResult> {
    const string Skipped = "No input changed since the last clean check, so the check did not run.";

    public override Task<Result<ChangesResult>> HandleAsync(IEnvelope<FindChangesCommand> envelope) {
        var command = envelope.Message;
        var hash = InputHash.Of(command.Block, command.Bin, command.Run);
        var same = StampFile.Read(command.Bin) == hash;
        Finding[] findings = same ? [new Finding(ChangesCodes.Same, FindingSeverity.Info, Skipped)] : [];
        return Task.FromResult<Result<ChangesResult>>(new ChangesResult(findings, same, hash));
    }
}
