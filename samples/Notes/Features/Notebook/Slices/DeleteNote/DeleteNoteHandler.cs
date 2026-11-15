namespace Notes.Features.Notebook.Slices.DeleteNote;

using Notes.Features.Notebook.Domain;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Infrastructure;

sealed class DeleteNoteHandler(IRepository<Note, NoteId> notes, IIntegrationEventPublisher events)
    : RequestHandler<DeleteNoteRequest, DeleteNoteResponse> {
    static readonly Error NotFound = new("notes.not_found", "The note does not exist.");

    public override async Task<Result<DeleteNoteResponse>> HandleAsync(Envelope<DeleteNoteRequest> envelope) {
        if (await notes.GetAsync(new NoteId(envelope.Message.Id), envelope.Cancellation) is not { } note) {
            return NotFound;
        }
        notes.Remove(note);
        await notes.SaveChangesAsync(envelope.Cancellation);
        await events.PublishAsync(new NoteDeleted(envelope.Message.Id), envelope.Cancellation);
        return new DeleteNoteResponse(envelope.Message.Id);
    }
}
