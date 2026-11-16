namespace VsaApp.Features.Notebook.Slices.Count;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class CountHandler : RequestHandler<CountRequest, CountResponse> {
    public override Task<Result<CountResponse>> HandleAsync(Envelope<CountRequest> envelope) =>
        Task.FromResult<Result<CountResponse>>(new CountResponse(0));
}
