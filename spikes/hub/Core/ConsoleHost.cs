namespace Spike.Core;

public sealed class ConsoleHost(string[] args) : AppHost {
    public override void Open(AppInstance app) => _ = RunOne(app);

    async Task RunOne(AppInstance app) {
        var exitCode = 1;
        try {
            var result = await app.Send(ToOperation(args)).ConfigureAwait(false);
            Console.WriteLine(result.Error ?? result.Value?.ToString());
            exitCode = result.Error is null ? 0 : 1;
        } catch (Exception e) {
            Console.Error.WriteLine(e.Message);
        }
        app.Stop(exitCode);
    }

    static Operation ToOperation(string[] args) {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var position = 0;
        for (var i = 1; i < args.Length; i++) {
            if (args[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length) {
                values[args[i][2..]] = args[++i];
            } else {
                values[$"{position++}"] = args[i];
            }
        }
        return new Operation(args.Length > 0 ? args[0] : "", values);
    }
}
