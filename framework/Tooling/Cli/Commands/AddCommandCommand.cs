using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add command <Name> -m <Module>: the command, the result and the handler. The first command of a module also
// gets the projects Contracts.Commands and Handlers.Commands, and the tool adds them to the solution.
static class AddCommandCommand {
    public static int Run(string name, string? module) {
        Names.Check(name, "command");
        if (module is null) {
            throw new VsaException("Give the module of the command: vsa add command <Name> -m <Module>");
        }
        var root = AppRoot.Find();
        module = AddRequestCommand.Module(root, module);
        var files = Files(root, name, module);
        Console.WriteLine($"Adding the command {name} to the module {module}:");
        FileWriter.Write(root, files);
        var projects = files.Where(f => f.Path.EndsWith(".csproj", StringComparison.Ordinal)).Select(f => f.Path).ToList();
        if (projects.Count > 0) {
            AppRoot.AddToSolution(root, projects);
        }
        return 0;
    }

    // The files of the slice, and each command project that the module does not have.
    internal static IReadOnlyList<(string Path, string Content)> Files(string root, string name, string module) {
        var names = new Dictionary<string, string> { [Templates.Command] = name, [Templates.Module] = module };
        return Templates.Files(Templates.Kind.Command, names, ToolVersion.Current)
            .Where(f => !f.Path.EndsWith(".csproj", StringComparison.Ordinal) || !File.Exists(Path.Combine(root, f.Path)))
            .ToList();
    }
}
