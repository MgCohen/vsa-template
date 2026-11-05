using VSA.Cli;
using VSA.Cli.Commands;

try {
    return args switch {
        ["new", "app", var name, .. var rest] => NewAppCommand.Run(name, Options.Read(rest, "--source")),
        ["add", "module", var name] => AddModuleCommand.Run(name),
        ["add", "request", var name, .. var rest] => AddRequestCommand.Run(name, Options.Read(rest, "-m", "--module") is [.., var module] ? module : null),
        ["add", "command", var name, .. var rest] => AddCommandCommand.Run(name, Options.Read(rest, "-m", "--module") is [.., var module] ? module : null),
        ["doctor"] => DoctorCommand.Run(),
        ["--version"] => Write(ToolVersion.Current),
        [] or ["-h" or "--help" or "help"] => Write(Usage),
        _ => throw new VsaException($"Unknown command: {string.Join(' ', args)}{Environment.NewLine}{Environment.NewLine}{Usage}"),
    };
} catch (VsaException e) {
    Console.Error.WriteLine($"vsa: {e.Message}");
    return 1;
}

static int Write(string text) {
    Console.WriteLine(text);
    return 0;
}

partial class Program {
    const string Usage = """
        Usage:
          vsa new app <Name> [--source <feed>]   Makes a new app in the folder <Name>. Each --source replaces the GitHub feed of the VSA packages.
          vsa add module <Name>                  Adds a module with all layers and a starter page.
          vsa add request <Name> -m <Module>     Adds a request, its response and its handler.
          vsa add command <Name> -m <Module>     Adds a command, its result and its handler.
          vsa doctor                             Makes sure that the tool, VSA.Sdk and the packages have one version.
          vsa --version                          Shows the version of the tool.
        """;
}
