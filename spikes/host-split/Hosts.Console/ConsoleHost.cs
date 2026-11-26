namespace Spike.Hosts.Console;

using Microsoft.Extensions.DependencyInjection;
using Spike.Kernels;

public class ConsoleHost : AppHost {
    readonly CancellationTokenSource stopping = new();
    ConsoleCancelEventHandler? cancel;
    Task? work;

    public override void Start(IServiceProvider services, AppStop stop) {
        cancel = (_, e) => {
            e.Cancel = true;
            stop.Request(1);
        };
        System.Console.CancelKeyPress += cancel;
        work = Task.Run(() => RunOne(services, stop, stopping.Token));
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

    protected override void Install(IServiceCollection services, AppTypes types, string[] args) =>
        services.AddSingleton(new CommandLine(args));

    static async Task RunOne(IServiceProvider services, AppStop stop, CancellationToken ct) {
        var line = services.GetRequiredService<CommandLine>();
        var operations = services.GetServices<Operation>().ToList();
        var operation = operations.SingleOrDefault(o => o.Name == line.Word);
        if (operation is null) {
            await System.Console.Error.WriteLineAsync(
                $"Name one request: {string.Join(", ", operations.Select(o => o.Name))}.").ConfigureAwait(false);
            stop.Request(2);
            return;
        }
        try {
            using var scope = services.CreateScope();
            var result = await operation.Invoke(scope.ServiceProvider, Bind(operation.Input, line), ct).ConfigureAwait(false);
            await System.Console.Out.WriteLineAsync(result.Error ?? result.Value?.ToString()).ConfigureAwait(false);
            stop.Request(result.ExitCode);
        } catch (Exception e) when (e is not OperationCanceledException) {
            await System.Console.Error.WriteLineAsync($"{operation.Name} failed: {e.Message}").ConfigureAwait(false);
            stop.Request(1);
        }
    }

    static object Bind(Type input, CommandLine line) {
        var constructor = input.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        return constructor.Invoke([.. constructor.GetParameters()
            .Select(p => Convert.ChangeType(line.Option(p.Name!) ?? p.DefaultValue, p.ParameterType,
                System.Globalization.CultureInfo.InvariantCulture))]);
    }
}
