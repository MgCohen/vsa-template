namespace VsaApp.Features.Notebook.Slices.GetNote;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;
using VsaApp.Features.Notebook.Slices.Shared;

sealed class GetNoteHandler : RequestHandler<GetNoteRequest, GetNoteResponse> {
    public override Task<Result<GetNoteResponse>> HandleAsync(Envelope<GetNoteRequest> envelope) =>
        Task.FromResult<Result<GetNoteResponse>>(new GetNoteResponse(new NoteTitle(envelope.Message.Title)));
}
