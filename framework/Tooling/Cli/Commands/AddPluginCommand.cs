using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add plugin <Name>: a plugin module with its root and a base command in Contracts/Commands.
// The root project of the app compiles the new folder, so the tool does not edit an existing file.
static class AddPluginCommand {
    public static int Run(string name) {
        Names.Check(name, "plugin");
        var root = AppRoot.Find();
        if (Directory.Exists(Path.Combine(root, "Plugins", name))) {
            throw new VsaException($"The plugin module {name} exists: Plugins/{name}");
        }
        // A module and a plugin module with the same name have the same namespace.
        if (Directory.Exists(Path.Combine(root, "Features", name))) {
            throw new VsaException($"The module {name} exists: Features/{name}. Use a different name for the plugin module.");
        }
        var files = Templates.Files(Templates.Kind.Plugin, new Dictionary<string, string> { [Templates.Plugin] = name }, ToolVersion.Current);
        Console.WriteLine($"Adding the plugin module {name}:");
        FileWriter.Write(root, files);
        Console.WriteLine($"Derive a module command from {name}Step<TResult>.");
        return 0;
    }
}
