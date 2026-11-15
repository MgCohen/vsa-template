using VSA.Cli;
using VSA.Cli.Commands;

try {
    return args switch {
        ["--version"] => Write(ToolVersion.Current),
        [] or ["-h" or "--help" or "help"] => Write(CommandList.Usage),
        _ => CommandList.Run(args),
    };
} catch (VsaException e) {
    Console.Error.WriteLine($"vsa: {e.Message}");
    return 1;
}

static int Write(string text) {
    Console.WriteLine(text);
    return 0;
}
