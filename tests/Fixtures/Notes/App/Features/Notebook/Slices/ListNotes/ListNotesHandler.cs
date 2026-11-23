namespace Notes.Features.Notebook.Slices.ListNotes;

using Notes.Features.Notebook.Domain;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using Umpire.Vsa.Infrastructure;

sealed class ListNotesHandler(IRepository<Note, NoteId> notes) : RequestHandler<ListNotesRequest, ListNotesResponse> {
    public override async Task<Result<ListNotesResponse>> HandleAsync(IEnvelope<ListNotesRequest> envelope) {
        var all = await notes.ListAsync(_ => true, envelope.Cancellation);
        return new ListNotesResponse([.. all.OrderBy(n => n.Title).Select(n => new NoteDto(n.Id.Value, n.Title))]);
    }
}
