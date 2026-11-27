namespace Ping.Features.Pong;

using Microsoft.Extensions.DependencyInjection;
using Ping.Features.Pong.Slices.Ping;
using Spike.Core;

public sealed class PongModule : Module {
    protected override void Configure(IServiceCollection services) =>
        services.AddValidator<PingValidator>();
}
