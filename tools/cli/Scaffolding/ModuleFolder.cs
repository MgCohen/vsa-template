namespace Umpire.Cli.Scaffolding;

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
            throw new UmpireException($"The module {module} does not exist. Add it with 'umpire add module {module}'.");
        }
        return Names.Check(folderNameOnDisk, "module");
    }

    public static void EnsureFree(string root, string name, ModuleKind kind) {
        if (name == RootProject.Name(root)) {
            throw new UmpireException($"The {Noun(kind)} name {name} is the name of the app. "
                + "A module with the app name makes usings that do not compile (CS0234). Use a different name.");
        }
        var own = kind == ModuleKind.Module ? Modules : Plugins;
        var other = kind == ModuleKind.Module ? Plugins : Modules;
        if (Directory.Exists(Path.Combine(root, own, name))) {
            throw new UmpireException($"The {Noun(kind)} {name} exists: {own}/{name}. Use a different name.");
        }
        if (Directory.Exists(Path.Combine(root, other, name))) {
            var otherKind = kind == ModuleKind.Module ? ModuleKind.Plugin : ModuleKind.Module;
            throw new UmpireException($"The {Noun(otherKind)} {name} exists: {other}/{name}. {SameNamespace}");
        }
    }

    static string Noun(ModuleKind kind) => kind == ModuleKind.Module ? "module" : "plugin module";
}
