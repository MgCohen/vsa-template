using SampleModule.Contracts.Requests.GetStatus;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace SampleModule.Handlers.Requests;

sealed class GetStatusHandler : RoutedHandler<GetStatusRequest, GetStatusResponse> {
    protected override Task<Result<GetStatusResponse>> HandleRequestAsync(GetStatusRequest request, CancellationToken ct) =>
        Task.FromResult<Result<GetStatusResponse>>(new GetStatusResponse("SampleModule", "Ready"));
}
