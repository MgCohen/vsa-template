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
public sealed record LayerEdge(string Target, Edge Edge, bool AnyModule);

// One layer part: its base project VSA.{Name} and the module or app projects of the layer.
// References: what the layer may use. Exposes: what may appear in its public signatures.
public sealed class Layer(string name) {
    readonly List<LayerEdge> references = [];
    readonly List<LayerEdge> exposes = [];

    public string Name { get; } = name;
    public string BaseAssembly => $"VSA.{Name}";
    public IReadOnlyList<LayerEdge> References => references;
    public IReadOnlyList<LayerEdge> Exposed => exposes;

    public Layer Reference(Layer target, Edge edge = Edge.Both, bool anyModule = false) => Reference(target.Name, edge, anyModule);

    // By name: for a layer that the map declares later.
    public Layer Reference(string target, Edge edge = Edge.Both, bool anyModule = false) {
        references.Add(new(target, edge, anyModule));
        return this;
    }

    public Layer Exposes(Layer target, Edge edge = Edge.Both) {
        exposes.Add(new(target.Name, edge, false));
        return this;
    }

    public override string ToString() => Name;
}
