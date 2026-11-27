namespace Ping.Features.Pong.Slices.Ping;

using Spike.Pack;

public sealed class PingHandler : RequestHandler<PingRequest, PongResponse> {
    public override Task<PongResponse> Handle(PingRequest request, CancellationToken ct) =>
        Task.FromResult(new PongResponse(request.Number));
}
