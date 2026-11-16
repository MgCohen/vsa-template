namespace Umpire.Cli.Scaffolding.Setup;

abstract record SetupResult {
    public sealed record Done : SetupResult;

    public sealed record Change(string File, string Content) : SetupResult;

    public sealed record ByHand(string Message) : SetupResult;
}
