using Versions.Domain.Shared;
using VSA.Architecture.Versions;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Versions.Slices.CheckVersions;

sealed class CheckVersionsHandler : CommandHandler<CheckVersionsCommand, OneVersion> {
    public override Task<Result<OneVersion>> HandleAsync(Envelope<CheckVersionsCommand> envelope) =>
        Task.FromResult(Check(envelope.Message));

    static Result<OneVersion> Check(CheckVersionsCommand command) {
        var expected = PackageVersion.Of(typeof(CheckVersionsHandler).Assembly);
        var slots = VersionSlots.InApp(Path.GetFullPath(command.AppRoot)).SelectMany(file => file.Slots)
            .Concat(VersionSlots.InBin(Path.GetFullPath(command.Bin)));
        var mismatches = VersionCheck.Mismatches(slots, expected);
        return mismatches is []
            ? new OneVersion(expected, [])
            : new Error(VersionCheck.Code, VersionCheck.Message(mismatches, expected));
    }
}
