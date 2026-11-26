namespace Greeter.Shared.Features.Counting.Slices.Count;

using Spike.Pack;

public sealed class CountHandler(Counter counter) : RequestHandler<CountRequest, CountResponse> {
    public override async Task<CountResponse> Handle(CountRequest request, CancellationToken ct) {
        await Task.Delay(request.DelayMs, ct).ConfigureAwait(false);
        return new CountResponse(counter.Next(), request.From);
    }
}
