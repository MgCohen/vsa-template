namespace VsaApp.Features.Notebook.Slices.Count;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class CountAgainHandler : RequestHandler<CountRequest, CountResponse> {
    public override Task<Result<CountResponse>> HandleAsync(Envelope<CountRequest> envelope) =>
        Task.FromResult<Result<CountResponse>>(new CountResponse(1));
}
