namespace Umpire.Cli.Host;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umpire.Hosts.Console;
using Umpire.Kernels;

public sealed class ToolHost : ConsoleHost {
    IReadOnlyList<string> line = [];
    ToolCommands commands = ToolCommands.Of([]);

    public ToolHost() {
        ModuleProvider = ToolModules.All();
    }

    public override IServiceProvider Build(string[] args, App app) {
        line = args;
        return base.Build([], app);
    }

    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        commands = ToolCommands.Of([.. typeof(ToolHost).Assembly.GetTypes()]);
        services.Replace(ServiceDescriptor.Singleton<IOperationConverter>(new ToolConverter(commands)));
    }

    protected override Operation OperationOf(IReadOnlyCollection<string> names, string[] args, HostArguments arguments) =>
        commands.OperationOf(line);

    protected override bool CancelsOnSignal(Operation operation) => commands.CancelsOnSignal(line);

    protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => [];
}
