namespace Ping.Features.Pong.Slices.Wait;

using Spike.Core;

public sealed record WaitRequest(int Milliseconds) : IRequest<WaitResponse>;
