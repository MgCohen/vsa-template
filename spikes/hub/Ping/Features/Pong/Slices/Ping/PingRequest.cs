namespace Ping.Features.Pong.Slices.Ping;

using Spike.Core;
using Spike.Pack;

public sealed record PingRequest(int Number) : IRequest<PongResponse>;
