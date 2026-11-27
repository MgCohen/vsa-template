namespace Umpire.Cli.Plugins.Commands.Domain.Shared;

public sealed record ToolCall(IReadOnlyList<string> Args, IReadOnlyList<CommandForm> Commands) {
    public CommandInput Input(CommandForm form) => CommandInput.Parse(form, [.. Args.Skip(form.Words.Count)]);
}
