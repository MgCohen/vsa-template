namespace Umpire.Kernels;

public sealed class FixedModules : IModuleProvider {
    readonly Module[] modules;

    public FixedModules(params Module[] modules) {
        Refuse(Duplicates(modules, module => module.GetType().FullName!), "type");
        Refuse(Duplicates(modules, module => module.Name), "name");
        this.modules = modules;
    }

    public IReadOnlyList<Module> Modules(ModuleScan scan) => modules;

    static List<string> Duplicates(Module[] modules, Func<Module, string> key) =>
        [.. modules.GroupBy(key).Where(same => same.Count() > 1).Select(same => same.Key)];

    static void Refuse(List<string> duplicates, string what) {
        if (duplicates.Count > 0) {
            throw new InvalidOperationException(
                $"FixedModules has more than one module with the {what} {string.Join(", ", duplicates)}. "
                + "Give each module one time.");
        }
    }
}
