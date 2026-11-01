using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add module <Name>: all layers, a status request with its handler, a presenter and a page.
// The host and the views find the new module by glob, so the tool does not edit an existing file.
static class AddModuleCommand {
    public static int Run(string name) {
        Names.Check(name, "module");
        var root = AppRoot.Find();
        if (Directory.Exists(Path.Combine(root, "Modules", name))) {
            throw new VsaException($"The module {name} exists: Modules/{name}");
        }
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = name }, ToolVersion.Current);
        Console.WriteLine($"Adding the module {name}:");
        FileWriter.Write(root, files);
        AppRoot.AddToSolution(root, files.Where(f => f.Path.EndsWith(".csproj", StringComparison.Ordinal)).Select(f => f.Path));
        Console.WriteLine($"The page of the module is /{Names.Kebab(name)}.");
        return 0;
    }
}
