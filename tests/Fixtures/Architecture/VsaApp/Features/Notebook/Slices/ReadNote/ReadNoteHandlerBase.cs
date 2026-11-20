namespace VsaApp.Features.Notebook.Slices.ReadNote;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

abstract class ReadNoteHandlerBase : RequestHandler<ReadNoteRequest, ReadNoteResponse> {
    public override Task<Result<ReadNoteResponse>> HandleAsync(IEnvelope<ReadNoteRequest> envelope) =>
        Task.FromResult<Result<ReadNoteResponse>>(new ReadNoteResponse());
}
