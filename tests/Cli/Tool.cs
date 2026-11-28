using Umpire.Cli;
using Umpire.Cli.Host;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

namespace Cli.Tests;

static class Tool {
    public static ToolCommands Commands { get; } = ToolCommands.Of([.. typeof(ToolHost).Assembly.GetTypes()]);

    public static int Run(params string[] args) {
        var app = new UmpireTool<TestToolHost>();
        app.Start([]);
        try {
            var result = app.Send(Commands.Request(args));
            return result.Error is { } error ? throw new UmpireException(error.Message) : result.Value is IExitCode code ? code.ExitCode : 0;
        } finally {
            app.Stop();
        }
    }

    public sealed class TestToolHost : LocalHost {
        public TestToolHost() {
            ModuleProvider = ToolModules.All();
        }

        protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => [];
    }
}
