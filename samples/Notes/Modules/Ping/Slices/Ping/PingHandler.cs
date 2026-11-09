using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Ping.Slices.Ping;

sealed class PingHandler : RequestHandler<PingRequest, PingResponse> {
    public override Task<Result<PingResponse>> HandleAsync(Envelope<PingRequest> envelope) =>
        Task.FromResult<Result<PingResponse>>(new PingResponse("pong"));
}
