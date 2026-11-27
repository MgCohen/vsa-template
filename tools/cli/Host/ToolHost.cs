namespace Umpire.Cli.Host;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Hosts.Console;
using Umpire.Kernels;

sealed class ToolHost : ConsoleHost {
    ToolLine line = new([]);

    public ToolHost() {
        ModuleProvider = new FixedModules();
    }

    public override IServiceProvider Build(string[] args, App app) {
        line = new ToolLine(args);
        return base.Build([.. line.Words], app);
    }

    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        services.AddSingleton(line);
    }

    protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => [];
}
