namespace Umpire.Cli.Features.Scaffold.Domain.Setup;

using Umpire.Files;

abstract record SetupResult {
    public sealed record Done : SetupResult;

    public sealed record Change(TextFile File) : SetupResult;

    public sealed record ByHand(string Message) : SetupResult;
}
