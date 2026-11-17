namespace Umpire.Cli.Commands.Bases;

using Umpire.Cli.Scaffolding;

abstract class AppCommand : ToolCommand {
    public sealed override int Run(CommandInput input) {
        CheckName(input);
        var block = RootProject.AppOfCurrentFolder();
        input.EnsureRequiredOptions();
        return Run(block, input);
    }

    protected virtual void CheckName(CommandInput input) { }

    protected abstract int Run(string block, CommandInput input);
}
