using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add plugin <Name>: a plugin module with its root and a base command in Contracts/Commands.
// Each module gets the layers of each plugin module from VSA.Sdk, so the tool does not edit an existing file.
static class AddPluginCommand {
    public static int Run(string name) {
        Names.Check(name, "plugin");
        var root = AppRoot.Find();
        if (Directory.Exists(Path.Combine(root, "Plugins", name))) {
            throw new VsaException($"The plugin module {name} exists: Plugins/{name}");
        }
        // A module and a plugin module with the same name have the same assembly names.
        if (Directory.Exists(Path.Combine(root, "Modules", name))) {
            throw new VsaException($"The module {name} exists: Modules/{name}. Use a different name for the plugin module.");
        }
        var files = Templates.Files(Templates.Kind.Plugin, new Dictionary<string, string> { [Templates.Plugin] = name }, ToolVersion.Current);
        Console.WriteLine($"Adding the plugin module {name}:");
        FileWriter.Write(root, files);
        AppRoot.AddToSolution(root, files.Where(f => f.Path.EndsWith(".csproj", StringComparison.Ordinal)).Select(f => f.Path));
        Console.WriteLine($"Each module gets the layers of {name}. Derive a module command from {name}Step<TResult>.");
        return 0;
    }
}
