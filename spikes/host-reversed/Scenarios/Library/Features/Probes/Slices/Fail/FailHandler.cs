namespace Greeter.Library.Features.Probes.Slices.Fail;

using Spike.Pack;

public sealed class FailHandler : RequestHandler<FailRequest, FailResponse> {
    public override Task<FailResponse> Handle(FailRequest request, CancellationToken ct) =>
        throw new ArgumentException(request.Reason, nameof(request));
}
