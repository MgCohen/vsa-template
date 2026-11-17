namespace Umpire.Cli.Scaffolding.Setup;

using Umpire.Files;

abstract record SetupResult {
    public sealed record Done : SetupResult;

    public sealed record Change(TextFile File) : SetupResult;

    public sealed record ByHand(string Message) : SetupResult;
}
