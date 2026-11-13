namespace VSA.Architecture;

public class Part {
    readonly List<Part> children = [];
    readonly List<Type> types = [];
    Depth? depth;
    Access? access;
    Boundary? boundary;
    string? folder;

    protected internal Part(Layer? layer, string? folder, Part? parent, bool group = false) {
        Layer = layer;
        this.folder = folder;
        Parent = parent;
        IsGroup = group;
    }

    public Layer? Layer { get; }
    public Part? Parent { get; }
    public bool IsGroup { get; }
    public bool HasFolder => folder is not null;
    public string? Assembly => Layer is External external ? external.Assembly
        : Parent?.Assembly is { } parent ? $"{parent}.{folder}" : null;
    public bool IsExternal => Assembly is not null;
    public IReadOnlyList<Part> Children => children;
    public IReadOnlyList<Type> Types => types;
    public string Name => Layer is null ? "(root)" : Layer.Display(Layer.GetType());
    public Depth? Depth => depth;
    public Access? Access => access;
    public Boundary? Boundary => boundary;
    public IEnumerable<Part> Groups => SelfAndAncestors().Where(p => p.IsGroup);

    public string Path {
        get {
            var own = IsGroup ? $"{folder}/{{{Name.ToLowerInvariant()}}}" : folder;
            if (Parent is null) {
                return own ?? "";
            }
            if (own is null) {
                return Parent.Path;
            }
            return Parent.Path.Length == 0 ? own : $"{Parent.Path}/{own}";
        }
    }

    internal Part Top => SelfAndAncestors().First(p => p.Parent?.Parent is null);

    public Part Add<T>() where T : Layer, new() {
        var layer = new T();
        return Attach(new Part(layer, layer.Folder, this));
    }

    public Part AddGroup<T>(string? folder = null) where T : Layer, new() {
        var layer = new T();
        return Attach(new Part(layer, folder ?? layer.GroupFolder ?? layer.Folder, this, group: true));
    }

    public Part AddGroup<T>(Boundary boundary, string? folder = null) where T : Layer, new() {
        var part = AddGroup<T>(folder);
        part.boundary = boundary;
        return part;
    }

    public PartGroup And<T>() where T : Layer, new() => new PartGroup([this]).And<T>();

    public Part InParentFolder() {
        if (IsExternal) {
            throw new InvalidOperationException(
                $"Define: {Name} is in the external {Assembly}. An external part has no InParentFolder, because its name is a DLL name segment. Remove InParentFolder.");
        }
        folder = null;
        return this;
    }

    public Part OfType<T>() => OfType(typeof(T));

    public Part OfType(Type type) {
        if (!types.Contains(type)) {
            types.Add(type);
        }
        return this;
    }

    public IEnumerable<Part> Walk() => new[] { this }.Concat(children.SelectMany(c => c.Walk()));

    public IEnumerable<Part> SelfAndAncestors() => new[] { this }.Concat(Ancestors());

    public bool Is(Type layer) => Layer?.GetType() == layer;

    internal IEnumerable<Part> Ancestors() {
        for (var p = Parent; p is not null; p = p.Parent) {
            yield return p;
        }
    }

    internal bool Under(Type layer) => SelfAndAncestors().Any(a => a.Is(layer));

    internal bool Holds(Part other) => other.SelfAndAncestors().Contains(this);

    Part Attach(Part child) {
        Block(child);
        var options = child.Layer!.Configured();
        child.depth = options.Depth;
        child.access = options.Access;
        children.Add(child);
        child.Layer!.Define(child);
        return child;
    }

    void Block(Part child) {
        var name = child.Name;
        if (child.Layer is External && child.IsGroup) {
            throw new InvalidOperationException(
                $"Define: AddGroup<{name}>() makes a group of an external. A DLL is not a group of folders. Use Add<{name}>().");
        }
        if (child.Layer is External && SelfAndAncestors().Any(p => p.IsGroup)) {
            throw new InvalidOperationException(
                $"Define: the external {name} is under the group {SelfAndAncestors().First(p => p.IsGroup).Name}. "
                + $"A DLL has no copy for each group instance. Add {name} outside the group.");
        }
        if (child.Layer is External && IsExternal) {
            throw new InvalidOperationException(
                $"Define: the external {name} is under the external {Assembly}. "
                + $"The child of an external gets its DLL name from its parent. Add {name} outside {Name}.");
        }
        if (child.IsGroup && IsExternal) {
            throw new InvalidOperationException(
                $"Define: AddGroup<{name}>() is under the external {Assembly}. DLL names have no instance segment. Use Add<{name}>().");
        }
    }
}
