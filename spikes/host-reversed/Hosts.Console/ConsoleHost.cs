namespace Spike.Hosts.Console;

using System.Globalization;
using Spike.Kernels;

public sealed class ConsoleHost(string[] args) : AppHost {
    readonly CommandLine line = new(args);
    readonly CancellationTokenSource stopping = new();
    ConsoleCancelEventHandler? cancel;
    Task? work;

    public override void Open(IOperations operations, AppStop stop) {
        cancel = (_, e) => {
            e.Cancel = true;
            stop.Request(130);
        };
        System.Console.CancelKeyPress += cancel;
        work = Task.Run(() => RunOne(operations, stop, stopping.Token));
    }

    public override void Dispose() {
        if (cancel is not null) {
            System.Console.CancelKeyPress -= cancel;
            cancel = null;
        }
        stopping.Cancel();
        try {
            work?.GetAwaiter().GetResult();
        } catch (OperationCanceledException) {
        } finally {
            stopping.Dispose();
            base.Dispose();
        }
    }

    async Task RunOne(IOperations operations, AppStop stop, CancellationToken ct) {
        var operation = operations.All.SingleOrDefault(o => o.Name == line.Word);
        if (operation is null) {
            await System.Console.Error.WriteLineAsync(
                $"Name one request: {string.Join(", ", operations.All.Select(o => o.Name))}.").ConfigureAwait(false);
            stop.Request(2);
            return;
        }
        object request;
        try {
            request = Bind(operation.Input);
        } catch (Exception e) when (e is FormatException or InvalidCastException) {
            await System.Console.Error.WriteLineAsync($"{operation.Name}: {e.Message}").ConfigureAwait(false);
            stop.Request(2);
            return;
        }
        var result = await operations.Invoke(request, ct).ConfigureAwait(false);
        var output = result.Error is null ? System.Console.Out : System.Console.Error;
        await output.WriteLineAsync(result.Error ?? result.Value?.ToString()).ConfigureAwait(false);
        stop.Request(result.ExitCode);
    }

    object Bind(Type input) {
        var constructor = input.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        return constructor.Invoke([.. constructor.GetParameters()
            .Select(p => Convert.ChangeType(line.Option(p.Name!) ?? p.DefaultValue, p.ParameterType, CultureInfo.InvariantCulture))]);
    }
}
