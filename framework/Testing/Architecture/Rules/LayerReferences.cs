using System.Reflection;
using VSA.Testing.Core;

namespace VSA.Testing.Architecture.Rules;

// VSA2004: each module project uses only what the layer map permits.
// It reads the assembly references of the compiled module projects. Only VSA bases and module projects are checked.
// VSA.Sdk sets the project references. This rule finds a use that the Sdk does not stop, for example a wrong map in the Sdk.
public sealed class LayerReferences(IReadOnlyList<Layer> layers) : IVsaRule {
    public LayerReferences() : this(Layers.All) { }

    public string Id => "VSA2004";
    public string Title => "Module projects use only what the layer map permits";

    public IEnumerable<Violation> Check(VsaApp app) {
        // Each module assembly of the app, with its module and its layer. The layer is the assembly name after "{root}.".
        var projects = new Dictionary<string, (AppModule Module, string Layer)>();
        foreach (var module in app.Modules) {
            var root = module.Root.GetName().Name!;
            projects[root] = (module, "Module");
            foreach (var layer in module.Layers) {
                var name = layer.GetName().Name!;
                projects[name] = (module, name.StartsWith(root + ".") ? name[(root.Length + 1)..] : name);
            }
        }

        foreach (var module in app.Modules) {
            foreach (var assembly in module.Assemblies) {
                var name = assembly.GetName().Name!;
                var source = projects[name].Layer;
                var layer = layers.FirstOrDefault(l => l.Name == source);
                if (layer is null) {
                    yield return new(Id, $"{name} is in layer {source}, which is not in the layer map. Move the project to a layer folder: {string.Join(", ", layers.Select(l => l.Name))}.");
                    continue;
                }
                foreach (var reference in assembly.GetReferencedAssemblies().Select(r => r.Name!).Order()) {
                    if (projects.TryGetValue(reference, out var target)) {
                        if (!Permits(layer, target.Layer, own: target.Module == module, module: true)) {
                            yield return new(Id, $"{name} ({layer}) uses {reference} ({target.Layer} of module {target.Module.Name}). The layer map does not permit it. Remove the use, or change the layer map.");
                        }
                    } else if (layers.FirstOrDefault(l => l.BaseAssembly == reference) is { } targetBase) {
                        if (!Permits(layer, targetBase.Name, own: true, module: false)) {
                            yield return new(Id, $"{name} ({layer}) uses {reference}, the base of {targetBase}. The layer map does not permit it. Remove the use, or change the layer map.");
                        }
                    }
                }
            }
        }
    }

    // A module project always uses its own base. Each edge permits the base of its target.
    // A module target needs an edge that is not BaseOnly, and the same module unless the edge is AnyModule.
    static bool Permits(Layer source, string target, bool own, bool module) =>
        (!module && target == source.Name)
        || source.References.Concat(source.Exposed).Any(e => e.Target == target
            && (!module || (e.Edge != Edge.BaseOnly && (own || e.AnyModule))));
}
