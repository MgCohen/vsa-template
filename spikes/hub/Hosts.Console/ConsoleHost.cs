namespace Spike.Hosts.Console;

using Spike.Core;

public sealed class ConsoleHost(string[] args) : AppHost {
    public override async Task<int> Run(IHub hub) {
        var result = await hub.Send(ToOperation(args)).ConfigureAwait(false);
        if (result.Error is { } error) {
            await System.Console.Error.WriteLineAsync(error).ConfigureAwait(false);
            return 1;
        }
        await System.Console.Out.WriteLineAsync(result.Value?.ToString()).ConfigureAwait(false);
        return 0;
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
