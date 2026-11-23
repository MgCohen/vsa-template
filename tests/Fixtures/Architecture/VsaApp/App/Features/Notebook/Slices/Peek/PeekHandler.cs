namespace VsaApp.Features.Notebook.Slices.Peek;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class PeekHandler : RequestHandler<PeekRequest, PeekResponse> {
    public override Task<Result<PeekResponse>> HandleAsync(IEnvelope<PeekRequest> envelope) =>
        Task.FromResult<Result<PeekResponse>>(new PeekResponse());
}
