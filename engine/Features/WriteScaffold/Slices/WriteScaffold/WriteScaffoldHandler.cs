namespace WriteScaffold.Slices.WriteScaffold;

using Scaffold.Infrastructure;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class WriteScaffoldHandler : CommandHandler<WriteScaffoldCommand, WriteScaffoldResult> {
    public override Task<Result<WriteScaffoldResult>> HandleAsync(Envelope<WriteScaffoldCommand> envelope) {
        var command = envelope.Message;
        FileSet.Write(command.BlockFolder, command.Plan.Files);
        IReadOnlyList<Finding> created = [.. command.Plan.Files
            .Select(file => new Finding(WriteScaffoldCodes.Created, FindingSeverity.Info, $"created {file.Path}"))];
        return Task.FromResult<Result<WriteScaffoldResult>>(new WriteScaffoldResult(created));
    }
}
