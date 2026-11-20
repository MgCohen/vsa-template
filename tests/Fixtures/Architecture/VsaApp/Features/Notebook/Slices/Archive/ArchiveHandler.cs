namespace VsaApp.Features.Notebook.Slices.Archive;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class ArchiveHandler : CommandHandler<ArchiveCommand, ArchiveResult> {
    public override Task<Result<ArchiveResult>> HandleAsync(IEnvelope<ArchiveCommand> envelope) =>
        Task.FromResult<Result<ArchiveResult>>(new ArchiveResult(true));
}
