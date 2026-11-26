namespace Greeter.Library.Features.Probes.Slices.Fail;

using Spike.Kernels;

public sealed record FailRequest(string Reason) : IRequest<FailResponse>;
