namespace VsaApp.Features.Notebook.Slices.Archive;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class ArchiveAuditHook : BeforeCommandHook<ArchiveCommand> {
    public override Task<ArchiveCommand> BeforeAsync(IEnvelope<ArchiveCommand> envelope) => Task.FromResult(envelope.Message);
}
