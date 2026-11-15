using Umpire.Cli;
using Umpire.Cli.Commands;

try {
    return args switch {
        ["--version"] => Write(ToolVersion.Current),
        [] or ["-h" or "--help" or "help"] => Write(CommandList.Usage),
        _ => CommandList.Run(args),
    };
} catch (UmpireException e) {
    Console.Error.WriteLine($"umpire: {e.Message}");
    return 1;
}

static int Write(string text) {
    Console.WriteLine(text);
    return 0;
}
