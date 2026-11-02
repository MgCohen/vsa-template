using System.Reflection;
using ArchUnitNET.Fluent;
using VSA.Testing.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace VSA.Testing.Architecture;

// One module project of the app: its assembly, its module and its layer.
// The layer is the assembly name after "{module root}.", for example Notes.Handlers.Requests is Handlers.Requests.
public sealed record ModuleProject(Assembly Assembly, AppModule Module, string Layer) {
    public string Name => Assembly.GetName().Name!;
}

// The layer map as ArchUnitNET rules: one rule for each module project.
// The rule forbids each VSA base and each module project that the map does not permit. Other assemblies are not checked.
public static class LayerRules {
    public static IReadOnlyList<ModuleProject> Projects(VsaApp app) => [.. app.Modules.SelectMany(module => {
        var root = module.Root.GetName().Name!;
        return module.Layers.Select(layer => {
            var name = layer.GetName().Name!;
            return new ModuleProject(layer, module, name.StartsWith(root + ".", StringComparison.Ordinal) ? name[(root.Length + 1)..] : name);
        }).Prepend(new ModuleProject(module.Root, module, "Module"));
    })];

    // The base assemblies of the layer map that load. The ArchUnitNET architecture needs them.
    public static IReadOnlyList<Assembly> Bases(IReadOnlyList<Layer>? layers = null) =>
        [.. (layers ?? Layers.All).Select(l => Load(l.BaseAssembly)).OfType<Assembly>()];

    // One message for each module project in a layer that the map does not have.
    public static IEnumerable<string> Unmapped(VsaApp app, IReadOnlyList<Layer>? layers = null) {
        layers ??= Layers.All;
        return Projects(app).Where(p => layers.All(l => l.Name != p.Layer)).Select(p =>
            $"{p.Name} is in layer {p.Layer}, which is not in the layer map. Move the project to a layer folder: {string.Join(", ", layers.Select(l => l.Name))}.");
    }

    public static IEnumerable<IArchRule> For(VsaApp app, IReadOnlyList<Layer>? layers = null) {
        layers ??= Layers.All;
        var projects = Projects(app);
        var bases = layers.Select(l => (Layer: l, Assembly: Load(l.BaseAssembly))).Where(b => b.Assembly is not null).ToList();
        foreach (var project in projects) {
            if (layers.FirstOrDefault(l => l.Name == project.Layer) is not { } layer) {
                continue;
            }
            List<Assembly> forbidden = [
                .. projects.Where(p => p != project && !Permits(layer, p.Layer, own: p.Module == project.Module, module: true)).Select(p => p.Assembly),
                .. bases.Where(b => !Permits(layer, b.Layer.Name, own: true, module: false)).Select(b => b.Assembly!),
            ];
            if (forbidden.Count == 0) {
                continue;
            }
            yield return Types().That().ResideInAssembly(project.Assembly)
                .Should().NotDependOnAny(Types().That().ResideInAssembly(forbidden[0], [.. forbidden.Skip(1)]))
                .Because($"the layer map does not permit {project.Layer} to use them. Remove the use, or change the layer map")
                .WithoutRequiringPositiveResults();
        }
    }

    // A module project always uses its own base. Each edge permits the base of its target.
    // A module target needs an edge that is not BaseOnly, and the same module unless the edge is AnyModule.
    static bool Permits(Layer source, string target, bool own, bool module) =>
        (!module && target == source.Name)
        || source.References.Concat(source.Exposed).Any(e => e.Target == target
            && (!module || (e.Edge != Edge.BaseOnly && (own || e.AnyModule))));

    static Assembly? Load(string name) {
        try {
            return Assembly.Load(name);
        } catch (FileNotFoundException) {
            return null;
        }
    }
}
