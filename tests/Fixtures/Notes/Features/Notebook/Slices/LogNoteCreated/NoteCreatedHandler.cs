namespace Notes.Features.Notebook.Slices.LogNoteCreated;

using Microsoft.Extensions.Logging;
using Notes.Features.Notebook.Slices.CreateNote;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Events;

sealed partial class NoteCreatedHandler(ILogger<NoteCreatedHandler> logger) : IntegrationEventHandler<NoteCreated> {
    public override Task<Result<Unit>> HandleAsync(Envelope<NoteCreated> envelope) {
        LogCreated(envelope.Message.NoteId, envelope.Message.Title);
        return Done;
    }

    // A LoggerMessage method: the analyzers (CA1848) require it.
    [LoggerMessage(LogLevel.Information, "Note {NoteId} created: {Title}")]
    partial void LogCreated(Guid noteId, string title);
}
