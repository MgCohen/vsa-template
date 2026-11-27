namespace Umpire.Hosts.Console;

using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    readonly CancellationTokenSource signalled = new();
    Operation? operation;
    AppSignals? signals;
    PosixSignalRegistration? terminate;
    Task? work;

    public override IServiceProvider Build(string[] args, App app) {
        var services = base.Build(args, app);
        if (OperationSends.NoConverterWarning(services) is { } warning) {
            System.Console.Error.WriteLine($"warning: {warning}");
        }
        var names = services.GetService<IOperationConverter>()?.Names ?? [];
        operation = OperationOf(names, args, services.GetRequiredService<HostArguments>());
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
        work = HostWork.Run(() => Run(services, selected, stop));
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

    protected virtual Operation OperationOf(IReadOnlyCollection<string> names, string[] args, HostArguments arguments) =>
        CommandLine.OperationOf(names, args, arguments);

    protected virtual bool CancelsOnSignal(Operation operation) => false;

    protected virtual int ExitCodeOf(IMessageResult result) =>
        result.Error is not null ? 1 : result.Value is IExitCode code ? code.ExitCode : 0;

    protected override HostArguments Arguments(string[] args) =>
        base.Arguments(CommandLine.Options(args));

    void CancelOperation() {
        try {
            signalled.Cancel();
        } catch (ObjectDisposedException) {
        }
    }

    async Task Run(IServiceProvider services, Operation selected, AppStop stop) {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token, signalled.Token);
        try {
            using var scope = services.CreateScope();
            var result = await scope.ServiceProvider.SendAsync(selected, cancel.Token).ConfigureAwait(false);
            if (result.Error is { } error) {
                await System.Console.Error.WriteLineAsync($"{error.Code}: {error.Message}").ConfigureAwait(false);
            }
            stop.Request(ExitCodeOf(result));
        } catch (OperationCanceledException) when (signalled.IsCancellationRequested && !stopping.IsCancellationRequested) {
            stop.Request();
        } catch (Exception e) when (e is not OperationCanceledException || !cancel.IsCancellationRequested) {
            await System.Console.Error.WriteLineAsync($"{selected.Name.Replace('/', ' ')} failed: {e.Message}").ConfigureAwait(false);
            stop.Request(1);
        }
    }
}
