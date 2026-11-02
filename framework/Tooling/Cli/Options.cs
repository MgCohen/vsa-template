namespace VSA.Cli;

static class Options {
    // The values of the one option that a command accepts. Any other argument is an error.
    // names: the names of the option, for example "-m" and "--module".
    public static IReadOnlyList<string> Read(string[] args, params string[] names) {
        var values = new List<string>();
        for (var i = 0; i < args.Length; i++) {
            if (Array.IndexOf(names, args[i]) < 0 || i + 1 >= args.Length) {
                throw new VsaException($"Unknown option or missing value: {args[i]}");
            }
            values.Add(args[++i]);
        }
        return values;
    }
}
