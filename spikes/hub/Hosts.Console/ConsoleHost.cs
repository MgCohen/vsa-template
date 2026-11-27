namespace Spike.Hosts.Console;

using Spike.Core;

public sealed class ConsoleHost(string[] args) : AppHost {
    ConsoleCancelEventHandler? cancel;

    public override Task Open(IOperations operations, AppStop stop) {
        cancel = (_, e) => {
            e.Cancel = true;
            stop.Request(130);
        };
        System.Console.CancelKeyPress += cancel;
        return Task.Run(() => RunOne(operations, stop));
    }

    public override ValueTask Close() {
        if (cancel is not null) {
            System.Console.CancelKeyPress -= cancel;
            cancel = null;
        }
        return default;
    }

    async Task RunOne(IOperations operations, AppStop stop) {
        var result = await operations.Send(ToOperation(args)).ConfigureAwait(false);
        if (result.Error is { } error) {
            await System.Console.Error.WriteLineAsync(error).ConfigureAwait(false);
            stop.Request(1);
            return;
        }
        await System.Console.Out.WriteLineAsync(result.Value?.ToString()).ConfigureAwait(false);
        stop.Request(0);
    }

    static Operation ToOperation(string[] args) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var position = 0;
        for (var i = 1; i < args.Length; i++) {
            if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length) {
                values[args[i].Substring(2)] = args[++i];
            } else {
                values[(position++).ToString(System.Globalization.CultureInfo.InvariantCulture)] = args[i];
            }
        }
        return new Operation(args.Length > 0 ? args[0] : "", values);
    }
}
