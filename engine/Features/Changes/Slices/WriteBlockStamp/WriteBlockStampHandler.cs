namespace Changes.Slices.WriteBlockStamp;

using Changes.Domain.Shared;
using Changes.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class WriteBlockStampHandler : CommandHandler<WriteBlockStampCommand, StampResult> {
    public override Task<Result<StampResult>> HandleAsync(IEnvelope<WriteBlockStampCommand> envelope) {
        var command = envelope.Message;
        return Task.FromResult<Result<StampResult>>(new StampResult(BlockStamp.Write(command.File, command.Hash)));
    }
}
