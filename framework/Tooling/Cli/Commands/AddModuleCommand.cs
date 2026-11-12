using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add module <Name>: all layers, a status request with its handler, a presenter and a page.
// The host and the views find the new module by glob, so the tool does not edit an existing file.
static class AddModuleCommand {
    public static int Run(string name) {
        Names.Check(name, "module");
        var root = AppRoot.Find();
        if (Directory.Exists(Path.Combine(root, "Features", name))) {
            throw new VsaException($"The module {name} exists: Features/{name}");
        }
        // A module and a plugin module with the same name have the same assembly names.
        if (Directory.Exists(Path.Combine(root, "Plugins", name))) {
            throw new VsaException($"The plugin module {name} exists: Plugins/{name}. Use a different name for the module.");
        }
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = name }, ToolVersion.Current);
        Console.WriteLine($"Adding the module {name}:");
        FileWriter.Write(root, files);
        AppRoot.AddToSolution(root, files.Where(f => f.Path.EndsWith(".csproj", StringComparison.Ordinal)).Select(f => f.Path));
        Console.WriteLine($"The page of the module is /{Names.Kebab(name)}.");
        return 0;
    }
}
