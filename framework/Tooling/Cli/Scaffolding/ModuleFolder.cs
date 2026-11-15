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

    public static void EnsureFree(string root, string name, ModuleKind kind) {
        var own = kind == ModuleKind.Module ? Modules : Plugins;
        var other = kind == ModuleKind.Module ? Plugins : Modules;
        if (Directory.Exists(Path.Combine(root, own, name))) {
            throw new VsaException($"The {Noun(kind)} {name} exists: {own}/{name}. Use a different name.");
        }
        if (Directory.Exists(Path.Combine(root, other, name))) {
            var otherKind = kind == ModuleKind.Module ? ModuleKind.Plugin : ModuleKind.Module;
            throw new VsaException($"The {Noun(otherKind)} {name} exists: {other}/{name}. {SameNamespace}");
        }
    }

    static string Noun(ModuleKind kind) => kind == ModuleKind.Module ? "module" : "plugin module";
}
