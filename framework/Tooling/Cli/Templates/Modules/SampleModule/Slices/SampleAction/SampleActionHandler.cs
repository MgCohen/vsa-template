using VSA.Domain;
using VSA.Handlers.Requests;

namespace SampleModule.Slices.SampleAction;

sealed class SampleActionHandler : RoutedHandler<SampleActionRequest, SampleActionResponse> {
    protected override Task<Result<SampleActionResponse>> HandleRequestAsync(SampleActionRequest request, CancellationToken ct) =>
        Task.FromResult<Result<SampleActionResponse>>(new SampleActionResponse());
}
