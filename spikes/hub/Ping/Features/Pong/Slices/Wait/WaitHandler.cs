namespace Ping.Features.Pong.Slices.Wait;

using Spike.Pack;

public sealed class WaitHandler : RequestHandler<WaitRequest, WaitResponse> {
    public override async Task<WaitResponse> Handle(WaitRequest request, CancellationToken ct) {
        await Task.Delay(request.Milliseconds, ct).ConfigureAwait(false);
        return new WaitResponse(request.Milliseconds);
    }
}
