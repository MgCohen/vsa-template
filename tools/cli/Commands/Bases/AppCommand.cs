using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands.Bases;

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
