using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add command <Name> -m <Module>: the command, the result and the handler.
static class AddCommandCommand {
    public static int Run(string name, string? module) {
        Names.Check(name, "command");
        if (module is null) {
            throw new VsaException("Give the module of the command: vsa add command <Name> -m <Module>");
        }
        var root = AppRoot.Find();
        module = AddRequestCommand.Module(root, module);
        Console.WriteLine($"Adding the command {name} to the module {module}:");
        FileWriter.Write(root, Files(name, module));
        return 0;
    }

    internal static IReadOnlyList<(string Path, string Content)> Files(string name, string module) {
        var names = new Dictionary<string, string> { [Templates.Command] = name, [Templates.Module] = module };
        return Templates.Files(Templates.Kind.Command, names, ToolVersion.Current);
    }
}
