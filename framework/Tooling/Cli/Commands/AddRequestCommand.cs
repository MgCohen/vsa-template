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
        if (!File.Exists(Path.Combine(root, "Modules", module, $"{module}.csproj"))) {
            throw new VsaException($"The module {module} does not exist. Add it with 'vsa add module {module}'.");
        }
        var names = new Dictionary<string, string> { [Templates.Request] = name, [Templates.Module] = module };
        Console.WriteLine($"Adding the request {name} to the module {module}:");
        FileWriter.Write(root, Templates.Files(Templates.Kind.Request, names, ToolVersion.Current));
        return 0;
    }
}
