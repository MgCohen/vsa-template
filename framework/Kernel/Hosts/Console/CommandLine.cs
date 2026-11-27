namespace Umpire.Hosts.Console;

using Umpire.Kernels;

static class CommandLine {
    internal static string[] Options(string[] args) => [.. args.SkipWhile(IsWord)];

    internal static IReadOnlyList<string> Words(string[] args) => [.. args.TakeWhile(IsWord)];

    internal static Operation OperationOf(IReadOnlyCollection<string> names, string[] args, HostArguments arguments) {
        var words = Words(args);
        var candidates = words.Count switch {
            0 => names.Count == 1 ? [.. names] : [],
            1 => names.Where(n => n == words[0] || n.EndsWith("/" + words[0], StringComparison.Ordinal)).ToList(),
            _ => names.Where(n => n == $"{words[0]}/{words[1]}").ToList(),
        };
        if (candidates.Count == 1) {
            return new Operation(candidates[0], arguments.All);
        }
        var listed = string.Join(", ", names.Select(n => n.Replace('/', ' ')).OrderBy(n => n, StringComparer.Ordinal));
        throw new InvalidOperationException(names.Count == 0
            ? "The console host found no request. Add a request and its handler."
            : $"Name one request on the command line: {listed}.");
    }

    static bool IsWord(string arg) => !arg.StartsWith("-", StringComparison.Ordinal);
}
