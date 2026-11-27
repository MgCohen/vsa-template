namespace Ping.Features.Pong.Slices.Ping;

public sealed record PongResponse(int Number) {
    public override string ToString() => $"pong {Number}";
}
