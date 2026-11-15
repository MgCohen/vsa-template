namespace Umpire.Architecture;

public class Part : IPartChain {
    readonly List<Part> children = [];
    readonly List<Type> types = [];
    Depth? depth;
    Access? access;
    Boundary? boundary;
    string? folder;
    string? assembly;

    protected internal Part(Concept? concept, string? folder, Part? parent, bool group = false) {
        Concept = concept;
        this.folder = folder;
        Parent = parent;
        IsGroup = group;
    }

    public Concept? Concept { get; }
    public Part? Parent { get; }
    public bool IsGroup { get; }
    public bool HasFolder => folder is not null;
    public string? Assembly => assembly ?? (Concept is External external ? external.Assembly
        : Parent?.Assembly is { } parent ? $"{parent}.{folder}" : null);
    public bool IsExternal => Assembly is not null;
    public IReadOnlyList<Part> Children => children;
    public IReadOnlyList<Type> Types => types;
    public string Name => Concept is null ? "(root)" : Concept.Display(Concept.GetType());
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

    public Part Add<T>() where T : Concept, new() {
        var concept = new T();
        return Attach(new Part(concept, concept is Module ? null : concept.Folder, this));
    }

    public PartGroup AddGroup<T>(string? folder = null) where T : Concept, new() => new([Group<T>(folder, null)]);

    public PartGroup AddGroup<T>(Boundary boundary, string? folder = null) where T : Concept, new() => new([Group<T>(folder, boundary)]);

    public PartGroup And<T>() where T : Concept, new() => new PartGroup([this]).And<T>();

    public Part InParentFolder() {
        if (IsExternal) {
            throw new InvalidOperationException(
                $"Define: {Name} is in the external {Assembly}. An external part has no InParentFolder, because its name is a DLL name segment. Remove InParentFolder.");
        }
        folder = null;
        return this;
    }

    public Part InAssembly(string name) {
        if (!IsExternal) {
            throw new InvalidOperationException(
                $"Define: {Name} is not in an external, so it has no DLL. Remove InAssembly, or add the part under an external.");
        }
        assembly = name;
        return this;
    }

    public Part OfType<T>() => OfType(typeof(T));

    public Part OfType(Type type) {
        if (!types.Contains(type)) {
            types.Add(type);
        }
        return this;
    }

    IPartChain IPartChain.Add<T>() => Add<T>();

    IPartChain IPartChain.InParentFolder() => InParentFolder();

    IPartChain IPartChain.OfType<T>() => OfType<T>();

    IPartChain IPartChain.OfType(Type type) => OfType(type);

    public IEnumerable<Part> Walk() => new[] { this }.Concat(children.SelectMany(c => c.Walk()));

    public IEnumerable<Part> SelfAndAncestors() => new[] { this }.Concat(Ancestors());

    public bool Is(Type concept) => Concept?.GetType() == concept;

    internal IEnumerable<Part> Ancestors() {
        for (var p = Parent; p is not null; p = p.Parent) {
            yield return p;
        }
    }

    internal bool Under(Type concept) => SelfAndAncestors().Any(a => a.Is(concept));

    internal bool Holds(Part other) => other.SelfAndAncestors().Contains(this);

    internal Part Group<T>(string? folder, Boundary? boundary) where T : Concept, new() {
        var concept = new T();
        var part = new Part(concept, folder ?? concept.GroupFolder ?? concept.Folder, this, group: true) { boundary = boundary };
        return Attach(part);
    }

    static void RefuseArchitectureOptions(Part child, Options options) {
        var names = new[] {
            options.DefaultPermission is null ? null : nameof(Options.DefaultPermission),
            options.UseSameConcept is null ? null : nameof(Options.UseSameConcept),
        };
        if (names.FirstOrDefault(n => n is not null) is { } name) {
            throw new InvalidOperationException(
                $"Define: the concept {child.Name} sets {name}. Only the architecture sets {name}. Move it to Configure of the architecture.");
        }
    }

    Part Attach(Part child) {
        Block(child);
        var options = child.Concept!.Configured();
        RefuseArchitectureOptions(child, options);
        child.depth = options.Depth;
        child.access = options.Access;
        child.boundary ??= options.Boundary;
        children.Add(child);
        child.Concept!.Define(child);
        return child;
    }

    void Block(Part child) {
        var name = child.Name;
        var group = SelfAndAncestors().FirstOrDefault(p => p.IsGroup);
        var external = child.Concept is External;
        if (!(external && (child.IsGroup || group is not null || IsExternal)) && !(child.IsGroup && IsExternal)) {
            return;
        }
        var call = child.IsGroup ? $"AddGroup<{name}>()" : $"Add<{name}>()";
        var place = IsExternal ? $"is under the external {Assembly}"
            : group is not null ? $"is under the group {group.Name}"
            : "makes a group of an external";
        throw new InvalidOperationException(
            $"Define: {call} {place}. A DLL name has no group segment, and the child of an external gets its DLL name from its parent. "
            + "So an external is not a group, and it is not under a group or another external. A group is not under an external. "
            + $"Use Add<{name}>(), and add each external outside the groups and the other externals.");
    }
}
