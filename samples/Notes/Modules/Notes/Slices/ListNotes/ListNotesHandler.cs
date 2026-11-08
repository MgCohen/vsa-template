using Notes.Domain;
using VSA.Domain;
using VSA.Handlers.Requests;
using VSA.Infrastructure;

namespace Notes.Slices.ListNotes;

sealed class ListNotesHandler(IRepository<Note, NoteId> notes) : RequestHandler<ListNotesRequest, ListNotesResponse> {
    protected override async Task<Result<ListNotesResponse>> HandleRequestAsync(ListNotesRequest request, CancellationToken ct) {
        var all = await notes.ListAsync(_ => true, ct);
        return new ListNotesResponse([.. all.OrderBy(n => n.Title).Select(n => new NoteDto(n.Id.Value, n.Title))]);
    }
}
