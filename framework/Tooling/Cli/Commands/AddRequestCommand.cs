using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa add request <Name> -m <Module>: the request, the response and the handler.
static class AddRequestCommand {
    public static int Run(string name, string? module) {
        Names.Check(name, "request");
        if (module is null) {
            throw new VsaException("Give the module of the request: vsa add request <Name> -m <Module>");
        }
        var root = AppRoot.Find();
        module = Module(root, module);
        var names = new Dictionary<string, string> { [Templates.Request] = name, [Templates.Module] = module };
        Console.WriteLine($"Adding the request {name} to the module {module}:");
        FileWriter.Write(root, Templates.Files(Templates.Kind.Request, names, ToolVersion.Current));
        return 0;
    }

    // The module name as its folder has it on disk. On Windows "-m billing" finds Features/Billing, and the namespaces
    // must use "Billing".
    internal static string Module(string root, string module) {
        var modules = Path.Combine(root, "Features");
        var folder = Directory.Exists(modules)
            ? Directory.EnumerateDirectories(modules).Select(Path.GetFileName).FirstOrDefault(d => string.Equals(d, module, StringComparison.OrdinalIgnoreCase))
            : null;
        if (folder is null) {
            throw new VsaException($"The module {module} does not exist. Add it with 'vsa add module {module}'.");
        }
        return Names.Check(folder, "module");
    }
}
