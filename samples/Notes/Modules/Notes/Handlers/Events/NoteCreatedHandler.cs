using Microsoft.Extensions.Logging;
using Notes.Contracts.Events;
using VSA.Handlers.Events;

namespace Notes.Handlers.Events;

sealed partial class NoteCreatedHandler(ILogger<NoteCreatedHandler> logger) : IntegrationEventHandler<NoteCreated> {
    public override Task HandleAsync(NoteCreated integrationEvent, CancellationToken ct) {
        LogCreated(integrationEvent.NoteId, integrationEvent.Title);
        return Task.CompletedTask;
    }

    // A LoggerMessage method: the analyzers (CA1848) require it.
    [LoggerMessage(LogLevel.Information, "Note {NoteId} created: {Title}")]
    partial void LogCreated(Guid noteId, string title);
}
