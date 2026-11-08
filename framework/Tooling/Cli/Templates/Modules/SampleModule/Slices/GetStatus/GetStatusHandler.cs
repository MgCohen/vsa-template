using VSA.Domain;
using VSA.Handlers.Requests;

namespace SampleModule.Slices.GetStatus;

sealed class GetStatusHandler : RequestHandler<GetStatusRequest, GetStatusResponse> {
    protected override Task<Result<GetStatusResponse>> HandleRequestAsync(GetStatusRequest request, CancellationToken ct) =>
        Task.FromResult<Result<GetStatusResponse>>(new GetStatusResponse("SampleModule", "Ready"));
}
