namespace Greeter.Features.Greeting.Slices.Greet;

using Spike.Kernels;

public sealed record GreetRequest(string Name = "world") : IRequest<GreetResponse>;
