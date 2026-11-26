namespace Greeter.Library.Features.Probes.Slices.Slow;

using Spike.Kernels;

public sealed record SlowRequest(int Milliseconds) : IRequest<SlowResponse>;
