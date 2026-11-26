namespace Umpire.Kernels;

static class HostArguments {
    internal static string[] Options(string[] args) {
        var options = new List<string>();
        for (var i = 0; i < args.Length; i++) {
            if (!IsOption(args[i])) {
                continue;
            }
            if (args[i].IndexOf('=') >= 0) {
                options.Add(args[i]);
            } else if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)) {
                options.Add(args[i]);
                options.Add(args[++i]);
            }
        }
        return [.. options];
    }

    static bool IsOption(string arg) => arg.Length > 2 && arg.StartsWith("--", StringComparison.Ordinal);
}
