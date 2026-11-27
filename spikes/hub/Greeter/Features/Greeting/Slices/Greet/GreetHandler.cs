namespace Greeter.Features.Greeting.Slices.Greet;

using Spike.Pack;

public sealed class GreetHandler : RequestHandler<GreetRequest, GreetResponse> {
    public override Task<GreetResponse> Handle(GreetRequest request, CancellationToken ct) =>
        Task.FromResult(new GreetResponse($"Hello, {request.Name}."));
}
