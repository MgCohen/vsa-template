using VSA.Contracts;
using VSA.Handlers.Requests;

namespace SampleModule.Slices.GetStatus;

sealed class GetStatusHandler : RequestHandler<GetStatusRequest, GetStatusResponse> {
    public override Task<Result<GetStatusResponse>> HandleAsync(Envelope<GetStatusRequest> envelope) =>
        Task.FromResult<Result<GetStatusResponse>>(new GetStatusResponse("SampleModule", "Ready"));
}
