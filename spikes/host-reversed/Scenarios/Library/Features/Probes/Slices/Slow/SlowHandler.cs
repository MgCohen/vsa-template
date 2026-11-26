namespace Greeter.Library.Features.Probes.Slices.Slow;

using Spike.Pack;

public sealed class SlowHandler : RequestHandler<SlowRequest, SlowResponse> {
    public override async Task<SlowResponse> Handle(SlowRequest request, CancellationToken ct) {
        await Task.Delay(request.Milliseconds, ct);
        return new SlowResponse($"slept {request.Milliseconds} ms");
    }
}
