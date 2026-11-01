namespace VSA.Cli;

static class Options {
    // The value of the one option that a command accepts, or null. Any other argument is an error.
    // names: the names of the option, for example "-m" and "--module".
    public static string? Read(string[] args, params string[] names) {
        string? value = null;
        for (var i = 0; i < args.Length; i++) {
            if (Array.IndexOf(names, args[i]) < 0 || i + 1 >= args.Length) {
                throw new VsaException($"Unknown option or missing value: {args[i]}");
            }
            value = args[++i];
        }
        return value;
    }
}
