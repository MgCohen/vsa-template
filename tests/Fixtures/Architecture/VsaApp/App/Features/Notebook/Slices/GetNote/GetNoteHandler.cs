namespace VsaApp.Features.Notebook.Slices.GetNote;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using VsaApp.Features.Notebook.Slices.Shared;

sealed class GetNoteHandler(IEnumerable<TitleCheck> checks) : RequestHandler<GetNoteRequest, GetNoteResponse> {
    public override Task<Result<GetNoteResponse>> HandleAsync(IEnvelope<GetNoteRequest> envelope) {
        var title = new NoteTitle(envelope.Message.Title);
        return Task.FromResult<Result<GetNoteResponse>>(checks.All(check => check.Passes(title))
            ? new GetNoteResponse(title)
            : new Error("notes.title", "The title fails a check."));
    }
}
