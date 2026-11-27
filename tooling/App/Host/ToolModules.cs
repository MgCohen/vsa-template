namespace Umpire.Cli.Host;

using Umpire.Cli.Features.Checks;
using Umpire.Cli.Features.Scaffold;
using Umpire.Cli.Features.Tool;
using Umpire.Cli.Features.Versions;
using Umpire.Cli.Plugins.Commands;
using Umpire.Kernels;

public static class ToolModules {
    public static FixedModules All() =>
        new(new CommandsPlugin(), new ToolModule(), new ScaffoldModule(), new ChecksModule(), new VersionsModule());
}
