namespace Greeter.Shared.Features.Counting.Slices.Count;

using Spike.Kernels;

public sealed record CountRequest(string From = "console", int DelayMs = 0) : IRequest<CountResponse>;
