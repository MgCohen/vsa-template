namespace Ping.Features.Pong.Slices.Ping;

using Microsoft.Extensions.DependencyInjection;
using Spike.Core;
using Spike.Pack;

public sealed record PingRequest(int Number) : IRequest<PongResponse>;

public sealed record PongResponse(int Number) {
    public override string ToString() => $"pong {Number}";
}

public sealed class PingHandler : RequestHandler<PingRequest, PongResponse> {
    public override Task<PongResponse> Handle(PingRequest request, CancellationToken ct) =>
        Task.FromResult(new PongResponse(request.Number));
}

public sealed class PingValidator : Validator<PingRequest> {
    public override string? Check(PingRequest message) => message.Number < 0 ? "ping: the number must not be negative." : null;
}

public sealed class PongModule : Module {
    protected override void Configure(IServiceCollection services) =>
        services.AddValidator<PingValidator, PingRequest>();
}
