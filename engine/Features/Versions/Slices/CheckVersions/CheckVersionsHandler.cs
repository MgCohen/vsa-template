namespace Versions.Slices.CheckVersions;

using Versions.Domain.Shared;
using Umpire.Engine.Versions;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckVersionsHandler : CommandHandler<CheckVersionsCommand, CheckVersionsResult> {
    public override Task<Result<CheckVersionsResult>> HandleAsync(Envelope<CheckVersionsCommand> envelope) =>
        Task.FromResult(Check(envelope.Message));

    static Result<CheckVersionsResult> Check(CheckVersionsCommand command) {
        var expected = PackageVersion.Of(typeof(CheckVersionsHandler).Assembly);
        var slots = VersionSlots.InApp(Path.GetFullPath(command.BlockFolder)).SelectMany(file => file.Slots)
            .Concat(VersionSlots.InBin(Path.GetFullPath(command.Bin)));
        var mismatches = VersionCheck.Mismatches(slots, expected);
        return mismatches is []
            ? new CheckVersionsResult([])
            : new Error(VersionCheck.Code, VersionCheck.Message(mismatches, expected));
    }
}
