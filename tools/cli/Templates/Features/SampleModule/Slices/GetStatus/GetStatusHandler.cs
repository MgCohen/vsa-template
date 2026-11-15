namespace SampleApp.Features.SampleModule.Slices.GetStatus;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class GetStatusHandler : RequestHandler<GetStatusRequest, GetStatusResponse> {
    public override Task<Result<GetStatusResponse>> HandleAsync(Envelope<GetStatusRequest> envelope) =>
        Task.FromResult<Result<GetStatusResponse>>(new GetStatusResponse("SampleModule", "Ready"));
}
