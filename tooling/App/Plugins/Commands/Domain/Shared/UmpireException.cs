namespace Umpire.Cli.Plugins.Commands.Domain.Shared;

public sealed class UmpireException(string message) : Exception(message) {
    public const string Code = "umpire";
}
