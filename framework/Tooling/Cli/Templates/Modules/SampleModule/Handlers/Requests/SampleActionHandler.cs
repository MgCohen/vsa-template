using SampleModule.Contracts.Requests.SampleAction;
using VSA.Domain;
using VSA.Handlers.Requests;

namespace SampleModule.Handlers.Requests;

sealed class SampleActionHandler : RoutedHandler<SampleActionRequest, SampleActionResponse> {
    protected override Task<Result<SampleActionResponse>> HandleRequestAsync(SampleActionRequest request, CancellationToken ct) =>
        Task.FromResult<Result<SampleActionResponse>>(new SampleActionResponse());
}
