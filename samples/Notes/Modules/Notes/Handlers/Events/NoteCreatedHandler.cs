using Microsoft.Extensions.Logging;
using Notes.Contracts.Events;
using VSA.Handlers.Events;

namespace Notes.Handlers.Events;

sealed class NoteCreatedHandler(ILogger<NoteCreatedHandler> logger) : IntegrationEventHandler<NoteCreated> {
    public override Task HandleAsync(NoteCreated integrationEvent, CancellationToken ct) {
        logger.LogInformation("Note {NoteId} created: {Title}", integrationEvent.NoteId, integrationEvent.Title);
        return Task.CompletedTask;
    }
}
