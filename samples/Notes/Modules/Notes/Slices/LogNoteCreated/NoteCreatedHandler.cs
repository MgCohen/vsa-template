using Microsoft.Extensions.Logging;
using Notes.Slices.CreateNote;
using VSA.Contracts;
using VSA.Handlers.Events;

namespace Notes.Slices.LogNoteCreated;

sealed partial class NoteCreatedHandler(ILogger<NoteCreatedHandler> logger) : IntegrationEventHandler<NoteCreated> {
    public override Task<Result<Unit>> HandleAsync(Envelope<NoteCreated> envelope) {
        LogCreated(envelope.Message.NoteId, envelope.Message.Title);
        return Done;
    }

    // A LoggerMessage method: the analyzers (CA1848) require it.
    [LoggerMessage(LogLevel.Information, "Note {NoteId} created: {Title}")]
    partial void LogCreated(Guid noteId, string title);
}
