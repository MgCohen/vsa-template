namespace Notes.Features.Ping.Slices.Ping;

using VSA.Contracts;
using VSA.Handlers.Requests;

sealed class PingHandler : RequestHandler<PingRequest, PingResponse> {
    public override Task<Result<PingResponse>> HandleAsync(Envelope<PingRequest> envelope) =>
        Task.FromResult<Result<PingResponse>>(new PingResponse("pong"));
}
