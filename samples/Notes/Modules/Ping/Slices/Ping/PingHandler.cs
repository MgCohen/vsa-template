using VSA.Domain;
using VSA.Handlers.Requests;

namespace Ping.Slices.Ping;

sealed class PingHandler : RequestHandler<PingRequest, PingResponse> {
    protected override Task<Result<PingResponse>> HandleRequestAsync(PingRequest request, CancellationToken ct) =>
        Task.FromResult<Result<PingResponse>>(new PingResponse("pong"));
}
