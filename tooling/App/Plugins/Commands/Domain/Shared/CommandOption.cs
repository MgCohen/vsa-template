namespace Umpire.Cli.Plugins.Commands.Domain.Shared;

public sealed record CommandOption(
    IReadOnlyList<string> Names, string Value, bool Required = false, bool Many = false) {
    public string Usage => Required ? $"{Names[0]} <{Value}>" : $"[{Names[0]} <{Value}>]";
}
