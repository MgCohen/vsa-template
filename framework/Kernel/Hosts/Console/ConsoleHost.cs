namespace Umpire.Hosts.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    Operation? operation;
    AppSignals? signals;
    Task? work;

    public override IServiceProvider Build(string[] args, App app) {
        var services = base.Build(args, app);
        operation = CommandLine.Select(services.GetServices<Operation>().ToList(), args);
        return services;
    }

    public override void Start(IServiceProvider services, AppStop stop) {
        signals = AppSignals.Listen(stop);
        work = HostWork.Run(() => CommandLine.Run(
            services, operation!, services.GetRequiredService<IConfiguration>(), stop, stopping.Token));
    }

    public override void Dispose() {
        signals?.Dispose();
        signals = null;
        stopping.Cancel();
        try {
            work?.GetAwaiter().GetResult();
        } catch (OperationCanceledException) {
        }
        stopping.Dispose();
        base.Dispose();
    }

    protected override IConfiguration Configuration(string[] args) =>
        base.Configuration(CommandLine.Options(args));
}
