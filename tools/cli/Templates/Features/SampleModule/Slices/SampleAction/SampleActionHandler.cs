namespace SampleApp.Features.SampleModule.Slices.SampleAction;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class SampleActionHandler : RequestHandler<SampleActionRequest, SampleActionResponse> {
    public override Task<Result<SampleActionResponse>> HandleAsync(Envelope<SampleActionRequest> envelope) =>
        Task.FromResult<Result<SampleActionResponse>>(new SampleActionResponse());
}
