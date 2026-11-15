using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands.Bases;

abstract class AppCommand : ToolCommand {
    public sealed override int Run(CommandInput input) => Run(AppRoot.Find(), input);

    protected abstract int Run(string root, CommandInput input);
}
