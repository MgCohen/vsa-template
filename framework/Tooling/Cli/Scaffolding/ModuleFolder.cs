namespace VSA.Cli.Scaffolding;

static class ModuleFolder {
    const string Modules = "Features";
    const string Plugins = "Plugins";
    const string SameNamespace =
        "A module and a plugin module with the same name have the same namespace. Use a different name.";

    public static string Find(string root, string module) {
        var modules = Path.Combine(root, Modules);
        var folderNameOnDisk = Directory.Exists(modules)
            ? Directory.EnumerateDirectories(modules)
                .Select(Path.GetFileName)
                .FirstOrDefault(folder => string.Equals(folder, module, StringComparison.OrdinalIgnoreCase))
            : null;
        if (folderNameOnDisk is null) {
            throw new VsaException($"The module {module} does not exist. Add it with 'vsa add module {module}'.");
        }
        return Names.Check(folderNameOnDisk, "module");
    }

    public static void EnsureFree(string root, string name) {
        if (Directory.Exists(Path.Combine(root, Modules, name))) {
            throw new VsaException($"The module {name} exists: {Modules}/{name}. {SameNamespace}");
        }
        if (Directory.Exists(Path.Combine(root, Plugins, name))) {
            throw new VsaException($"The plugin module {name} exists: {Plugins}/{name}. {SameNamespace}");
        }
    }
}
