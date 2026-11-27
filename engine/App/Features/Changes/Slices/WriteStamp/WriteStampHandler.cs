namespace Changes.Slices.WriteStamp;

using Changes.Domain.Shared;
using Changes.Infrastructure;
using Operations.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class WriteStampHandler(RunOptions options) : CommandHandler<WriteStampCommand, StampResult> {
    public override Task<Result<StampResult>> HandleAsync(IEnvelope<WriteStampCommand> envelope) {
        var command = envelope.Message;
        var file = StampFile.Write(options.StampFolder(command.Bin), command.Hash);
        return Task.FromResult<Result<StampResult>>(new StampResult(file));
    }
}
