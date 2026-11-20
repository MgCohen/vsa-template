namespace Changes.Slices.WriteStamp;

using Changes.Domain.Shared;
using Changes.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class WriteStampHandler : CommandHandler<WriteStampCommand, StampResult> {
    public override Task<Result<StampResult>> HandleAsync(IEnvelope<WriteStampCommand> envelope) {
        var command = envelope.Message;
        var file = StampFile.Write(command.Bin, command.Hash);
        return Task.FromResult<Result<StampResult>>(new StampResult(file));
    }
}
