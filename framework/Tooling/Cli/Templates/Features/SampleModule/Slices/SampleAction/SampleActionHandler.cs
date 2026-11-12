using VSA.Contracts;
using VSA.Handlers.Requests;

namespace SampleModule.Slices.SampleAction;

sealed class SampleActionHandler : RequestHandler<SampleActionRequest, SampleActionResponse> {
    public override Task<Result<SampleActionResponse>> HandleAsync(Envelope<SampleActionRequest> envelope) =>
        Task.FromResult<Result<SampleActionResponse>>(new SampleActionResponse());
}
