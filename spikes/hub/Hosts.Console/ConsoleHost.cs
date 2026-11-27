namespace Spike.Hosts.Console;

using Spike.Core;

public sealed class ConsoleHost(string[] args) : AppHost {
    public override async Task<int> Run(IOperations operations) {
        var result = await operations.Send(ToOperation(args)).ConfigureAwait(false);
        if (result.Error is { } error) {
            await System.Console.Error.WriteLineAsync(error).ConfigureAwait(false);
            return 1;
        }
        await System.Console.Out.WriteLineAsync(result.Value?.ToString()).ConfigureAwait(false);
        return 0;
    }

    static Operation ToOperation(string[] args) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i + 1 < args.Length; i += 2) {
            values[args[i].TrimStart('-')] = args[i + 1];
        }
        return new Operation(args.Length > 0 ? args[0] : "", values);
    }
}
