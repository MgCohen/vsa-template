namespace Umpire.Cli.Commands.Bases;

using Umpire.Cli.Scaffolding;

abstract class AppCommand : ToolCommand {
    public sealed override int Run(CommandInput input) {
        CheckName(input);
        var root = AppRoot.Find();
        input.EnsureRequiredOptions();
        return Run(root, input);
    }

    protected virtual void CheckName(CommandInput input) { }

    protected abstract int Run(string root, CommandInput input);
}
