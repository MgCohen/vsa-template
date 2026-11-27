namespace Umpire.Cli.Host;

using Microsoft.Extensions.DependencyInjection;
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
        var operations = services
            .Where(d => d.ServiceType == typeof(Operation) && d.ImplementationInstance is Operation)
            .ToList();
        commands = ToolCommands.Of([.. operations.Select(d => ((Operation)d.ImplementationInstance!).Input)]);
        foreach (var descriptor in operations) {
            if (commands.Wrap((Operation)descriptor.ImplementationInstance!, () => line) is { } wrapped) {
                services.Remove(descriptor);
                services.AddSingleton(wrapped);
            }
        }
    }

    protected override Operation Select(IReadOnlyList<Operation> operations, string[] args) {
        var form = commands.Select(line);
        return base.Select(operations, [form.Group, form.Name]);
    }

    protected override bool CancelsOnSignal(Operation operation) => commands.CancelsOnSignal(line);

    protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => [];
}
