namespace Ping.Features.Pong.Slices.Wait;

public sealed record WaitResponse(int Milliseconds) {
    public override string ToString() => $"waited {Milliseconds} ms";
}
