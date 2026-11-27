namespace Greeter.Features.Greeting.Slices.Greet;

using Spike.Pack;

public sealed record GreetRequest(string Name) : IRequest<GreetResponse>;
