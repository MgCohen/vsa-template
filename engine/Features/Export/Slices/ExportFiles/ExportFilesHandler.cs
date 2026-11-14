using Export.Infrastructure;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Export.Slices.ExportFiles;

sealed class ExportFilesHandler : CommandHandler<ExportFilesCommand, ExportFilesResult> {
    public const string Written = "VSA3005";

    public override Task<Result<ExportFilesResult>> HandleAsync(Envelope<ExportFilesCommand> envelope) {
        var command = envelope.Message;
        var folder = Path.GetFullPath(command.Folder);
        var summary = AppFiles.Write(command.App, folder);
        var message = $"ExportFiles wrote {folder}: {AppFiles.DefinitionFile} ({summary.TopParts} top parts, {summary.Rules} rules), "
            + $"{AppFiles.ArchitectureFile} ({summary.Types} types).";
        return Task.FromResult<Result<ExportFilesResult>>(
            new ExportFilesResult(folder, [new Finding(Written, FindingSeverity.Info, message)]));
    }
}
