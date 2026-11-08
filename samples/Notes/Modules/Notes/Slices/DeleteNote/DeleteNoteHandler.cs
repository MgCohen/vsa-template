using Notes.Domain;
using VSA.Contracts.Events;
using VSA.Domain;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace Notes.Slices.DeleteNote;

sealed class DeleteNoteHandler(IRepository<Note, NoteId> notes, IIntegrationEventPublisher events)
    : RequestHandler<DeleteNoteRequest, DeleteNoteResponse> {
    protected override async Task<Result<DeleteNoteResponse>> HandleRequestAsync(DeleteNoteRequest request, CancellationToken ct) {
        if (await notes.GetAsync(new NoteId(request.Id), ct) is not { } note) {
            return NoteErrors.NotFound;
        }
        notes.Remove(note);
        await notes.SaveChangesAsync(ct);
        await events.PublishAsync(new NoteDeleted(request.Id), ct);
        return new DeleteNoteResponse(request.Id);
    }
}
