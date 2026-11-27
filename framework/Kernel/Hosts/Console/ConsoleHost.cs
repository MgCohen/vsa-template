namespace Umpire.Hosts.Console;

using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using Umpire.Kernels;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    readonly CancellationTokenSource signalled = new();
    Operation? operation;
    AppSignals? signals;
    PosixSignalRegistration? terminate;
    Task? work;

    public override IServiceProvider Build(string[] args, App app) {
        var services = base.Build(args, app);
        operation = Select(services.GetServices<Operation>().ToList(), args);
        return services;
    }

    public override void Start(IServiceProvider services, AppStop stop) {
        var selected = operation!;
        Action onSignal = CancelsOnSignal(selected) ? CancelOperation : () => stop.Request();
        signals = AppSignals.Listen(onSignal);
        terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => {
            context.Cancel = true;
            onSignal();
        });
        var arguments = services.GetRequiredService<HostArguments>();
        work = HostWork.Run(() => Run(services, selected, arguments, stop));
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
        signalled.Dispose();
        base.Dispose();
    }

    protected virtual Operation Select(IReadOnlyList<Operation> operations, string[] args) =>
        CommandLine.Select(operations, args);

    protected virtual bool CancelsOnSignal(Operation operation) => false;

    protected override HostArguments Arguments(string[] args) =>
        base.Arguments(CommandLine.Options(args));

    void CancelOperation() {
        try {
            signalled.Cancel();
        } catch (ObjectDisposedException) {
        }
    }

    async Task Run(IServiceProvider services, Operation selected, HostArguments arguments, AppStop stop) {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token, signalled.Token);
        try {
            await CommandLine.Run(services, selected, arguments, stop, cancel.Token).ConfigureAwait(false);
        } catch (OperationCanceledException) when (signalled.IsCancellationRequested && !stopping.IsCancellationRequested) {
            stop.Request();
        }
    }
}
