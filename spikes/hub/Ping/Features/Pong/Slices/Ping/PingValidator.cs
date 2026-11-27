namespace Ping.Features.Pong.Slices.Ping;

using Spike.Core;

public sealed class PingValidator : Validator<PingRequest> {
    protected override string? Check(PingRequest message) =>
        message.Number < 0 ? "ping: the number must not be negative." : null;
}
