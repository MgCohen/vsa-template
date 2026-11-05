using System.Collections.Concurrent;
using System.Reflection;
using VSA.Module;
using VSA.Testing.Core;

namespace VSA.Testing.Architecture;

// One module project of the app: its assembly, its module root and its layer.
// The layer is the assembly name after "{module root}.", for example Notes.Handlers.Requests is Handlers.Requests.
sealed record ModuleProject(Assembly Assembly, ModuleRoot Module, string Layer) {
    public string Name => Assembly.GetName().Name!;
}

// The data of the layer map rule (VSA2001). The ArchUnitNET rule is in VsaArchitectureTests. Other assemblies are not checked.
static class LayerRules {
    // The base assembly of each layer name, or null if it does not load. Each base loads one time.
    static readonly ConcurrentDictionary<string, Assembly?> BaseAssemblies = new();

    // The module roots and the layer projects that each root recorded at build time.
    public static IReadOnlyList<ModuleProject> Projects(VsaApp app) => [.. app.Modules.SelectMany(module =>
        module.Layers.Select(name => new ModuleProject(
            Assembly.Load(name), module, name.StartsWith(module.Name + ".", StringComparison.Ordinal) ? name[(module.Name.Length + 1)..] : name))
        .Prepend(new ModuleProject(module.Assembly, module, "Module")))];

    // The base assemblies of the layer map that load.
    public static IReadOnlyList<Assembly> Bases(IReadOnlyList<Layer> layers) =>
        [.. layers.Select(l => Base(l)).OfType<Assembly>()];

    // One message for each module project in a layer that the map does not have.
    public static IEnumerable<string> Unmapped(VsaApp app, IReadOnlyList<Layer> layers) =>
        Projects(app).Where(p => layers.All(l => l.Name != p.Layer)).Select(p =>
            $"{p.Name} is in layer {p.Layer}, which is not in the layer map. Rename the project to <module>.<layer>, with one of these layers: {string.Join(", ", layers.Select(l => l.Name))}.");

    // The assemblies that each module project must not use: each VSA base and each module project that the map does not permit.
    // A project with no forbidden assembly is not in the list.
    public static IEnumerable<(ModuleProject Project, IReadOnlyList<Assembly> Forbidden)> Forbidden(VsaApp app, IReadOnlyList<Layer> layers) {
        var projects = Projects(app);
        foreach (var project in projects) {
            if (layers.FirstOrDefault(l => l.Name == project.Layer) is not { } layer) {
                continue;
            }
            List<Assembly> forbidden = [
                .. projects.Where(p => p != project && !Permits(layer, project, p)).Select(p => p.Assembly),
                .. layers.Where(l => !Permits(layer, l.Name, own: true, module: false)).Select(Base).OfType<Assembly>(),
            ];
            if (forbidden.Count > 0) {
                yield return (project, forbidden);
            }
        }
    }

    // A Shared module is a base, as the framework is: each project may use what the map permits of a base.
    // A Shared module uses no project of a module that is not Shared.
    // Listed: the target module is in the VsaModuleUses list of the source module root. The case of the name does not count,
    // as for MSBuild on Windows.
    static bool Permits(Layer layer, ModuleProject source, ModuleProject target) =>
        target.Module == source.Module ? Permits(layer, target.Layer, own: true, module: true)
        : target.Module.IsShared ? Permits(layer, target.Layer, own: true, module: false)
        : !source.Module.IsShared && Permits(layer, target.Layer, own: false, module: true, listed: source.Module.Uses.Contains(target.Module.Name, StringComparer.OrdinalIgnoreCase));

    // A module project always uses its own base. Each edge permits the base of its target.
    // A module target needs an edge that is not BaseOnly, and the same module unless the edge is AnyModule,
    // or the edge is Listed and the source module lists the target module.
    static bool Permits(Layer source, string target, bool own, bool module, bool listed = false) =>
        (!module && target == source.Name)
        || source.References.Any(e => e.Target == target
            && (!module || (e.Edge != Edge.BaseOnly && (own || e.AnyModule || (e.Listed && listed)))));

    static Assembly? Base(Layer layer) => BaseAssemblies.GetOrAdd(layer.BaseAssembly, static name => {
        try {
            return Assembly.Load(name);
        } catch (FileNotFoundException) {
            return null;
        }
    });
}
