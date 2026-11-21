namespace Umpire.Kernels;

using System.Reflection;

public sealed class ModuleScan {
    readonly AppAssemblies scan;
    IReadOnlyList<ModuleRoot>? roots;

    internal ModuleScan(AppAssemblies scan) {
        this.scan = scan;
    }

    internal IReadOnlyList<ModuleRoot> Roots => roots ??= RootsIn(scan.OfModules());

    public IReadOnlyList<Module> All() => [.. Roots.Select(root => root.Create())];

    public IReadOnlyList<Module> In(IEnumerable<Assembly> assemblies) =>
        [.. RootsIn(assemblies).Select(root => root.Create())];

    IReadOnlyList<ModuleRoot> RootsIn(IEnumerable<Assembly> assemblies) => [.. assemblies
        .Distinct()
        .SelectMany(assembly => ModuleRoot.In(assembly, scan.TypesOf(assembly)))
        .OrderBy(root => root.Name, StringComparer.Ordinal)];
}
