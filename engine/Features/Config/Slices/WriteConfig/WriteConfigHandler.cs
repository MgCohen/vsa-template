namespace Config.Slices.WriteConfig;

using Config.Domain.Shared;
using Config.Infrastructure;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class WriteConfigHandler : CommandHandler<WriteConfigCommand, WriteConfigResult> {
    public override Task<Result<WriteConfigResult>> HandleAsync(Envelope<WriteConfigCommand> envelope) {
        var config = UmpireConfig.Find(envelope.Message.Folder);
        var copy = config.FilePath is null ? null : BlockConfigCopy.Write(config);
        return Task.FromResult<Result<WriteConfigResult>>(new WriteConfigResult([], copy));
    }
}
