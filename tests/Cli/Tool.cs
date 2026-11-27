using Umpire.Cli;
using Umpire.Cli.Host;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

namespace Cli.Tests;

static class Tool {
    public static ToolCommands Commands { get; } = ToolCommands.Of([.. typeof(ToolHost).Assembly.GetTypes()]);

    public static int Run(params string[] args) {
        var host = new TestToolHost();
        var running = new UmpireTool().Run(host);
        try {
            var result = host.Send(Commands.Request(args));
            return result.Error is { } error ? throw new UmpireException(error.Message) : result.Value is IExitCode code ? code.ExitCode : 0;
        } finally {
            running.Stop();
        }
    }

    public sealed class TestToolHost : LocalHost {
        public TestToolHost() {
            ModuleProvider = ToolModules.All();
        }

        protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => [];
    }
}
