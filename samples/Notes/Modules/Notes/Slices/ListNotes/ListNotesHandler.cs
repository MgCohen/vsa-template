using Notes.Domain;
using VSA.Contracts;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace Notes.Slices.ListNotes;

sealed class ListNotesHandler(IRepository<Note, NoteId> notes) : RequestHandler<ListNotesRequest, ListNotesResponse> {
    public override async Task<Result<ListNotesResponse>> HandleAsync(Envelope<ListNotesRequest> envelope) {
        var all = await notes.ListAsync(_ => true, envelope.Cancellation);
        return new ListNotesResponse([.. all.OrderBy(n => n.Title).Select(n => new NoteDto(n.Id.Value, n.Title))]);
    }
}
