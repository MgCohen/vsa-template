namespace Umpire.Hosts.Console;

using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using Umpire.Kernels;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    Operation? operation;
    AppSignals? signals;
    PosixSignalRegistration? terminate;
    Task? work;

    public override IServiceProvider Build(string[] args, App app) {
        var services = base.Build(args, app);
        operation = CommandLine.Select(services.GetServices<Operation>().ToList(), args);
        return services;
    }

    public override void Start(IServiceProvider services, AppStop stop) {
        signals = AppSignals.Listen(stop);
        terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => {
            context.Cancel = true;
            stop.Request();
        });
        work = HostWork.Run(() => CommandLine.Run(
            services, operation!, services.GetRequiredService<HostArguments>(), stop, stopping.Token));
    }

    public override void Dispose() {
        signals?.Dispose();
        signals = null;
        terminate?.Dispose();
        terminate = null;
        stopping.Cancel();
        try {
            work?.GetAwaiter().GetResult();
        } catch (OperationCanceledException) {
        }
        stopping.Dispose();
        base.Dispose();
    }

    protected override HostArguments Arguments(string[] args) =>
        base.Arguments(CommandLine.Options(args));
}
