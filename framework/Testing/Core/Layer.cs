namespace VSA.Testing.Core;

// Which projects of the target layer an edge permits.
public enum Edge {
    // A base project may use the base of X. A module project may use X and the base of X.
    Both,
    // Each project may use only the base of X.
    BaseOnly,
    // A base project may use nothing of X. A module project may use X and the base of X.
    ModuleOnly,
}

// One edge to a target layer, by name. AnyModule: a module project may use X of all modules, not only its own.
// Listed: a module project may use X of its own module and of each module that its root lists in VsaModuleUses.
public sealed record LayerEdge(string Target, Edge Edge, bool AnyModule, bool Listed = false);

// One layer part: its base project VSA.{Name} and the module or app projects of the layer.
// References: what the layer may use.
public sealed class Layer(string name) {
    readonly List<LayerEdge> references = [];

    public string Name { get; } = name;
    public string BaseAssembly => $"VSA.{Name}";
    public IReadOnlyList<LayerEdge> References => references;

    public Layer Reference(Layer target, Edge edge = Edge.Both, bool anyModule = false, bool listed = false) =>
        Reference(target.Name, edge, anyModule, listed);

    // By name: for a layer that the map declares later, or for the layer itself.
    public Layer Reference(string target, Edge edge = Edge.Both, bool anyModule = false, bool listed = false) {
        references.Add(new(target, edge, anyModule, listed));
        return this;
    }

    public override string ToString() => Name;
}
