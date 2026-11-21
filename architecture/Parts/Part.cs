namespace Umpire.Architecture;

using Umpire.Architecture.Blocks;

public class Part : IPartChain {
    readonly List<Part> children = [];
    readonly List<Type> types = [];
    readonly Dictionary<Type, bool> requiredAtCallSite = [];
    Options options = new();
    string? folder;
    string? assembly;

    protected internal Part(Concept? concept, string? folder, Part? parent, bool group = false, bool blockPlace = false) {
        Concept = concept;
        this.folder = folder;
        Parent = parent;
        IsGroup = group;
        IsBlockPlace = blockPlace;
    }

    public Concept? Concept { get; }
    public Part? Parent { get; }
    public bool IsGroup { get; }
    public bool IsBlockPlace { get; }
    public bool HasFolder => folder is not null;
    public string? Assembly => assembly ?? (Concept is External external ? external.Assembly
        : Parent?.Assembly is { } parent ? $"{parent}.{folder}" : null);
    public bool IsExternal => Assembly is not null;
    public IReadOnlyList<Part> Children => children;
    public IReadOnlyList<Type> Types => types;
    public string Name => Concept is null ? "(block)" : Concept.Display(Concept.GetType());
    public Options Options => options.Over(new Options());
    public IEnumerable<Part> Groups => SelfAndAncestors().Where(p => p.IsGroup);
    internal NamePattern? NamePattern { get; private set; }

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

    public Part AddBlock<T>(string? folder = null) where T : Concept, new() {
        var concept = new T();
        return Attach(new Part(concept, folder ?? concept.Folder, this, blockPlace: true));
    }

    public Part AddBlockGroup<T>(string? folder = null) where T : Concept, new() {
        var concept = new T();
        return Attach(new Part(concept, folder ?? concept.GroupFolder ?? concept.Folder, this, group: true, blockPlace: true));
    }

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

    public Part OfType<T>(bool? required = null) => OfType(typeof(T), required);

    public Part OfType(Type type, bool? required = null) {
        if (!types.Contains(type)) {
            types.Add(type);
        }
        if (required is { } value) {
            requiredAtCallSite[type] = value;
        }
        return this;
    }

    public Part NameStartsWith(string text) => WithName((NamePattern ?? NamePattern.Any).WithStart(text));

    public Part NameEndsWith(string text) => WithName((NamePattern ?? NamePattern.Any).WithEnd(text));

    public Part NamedAfterGroup(string suffix = "") => WithName((NamePattern ?? NamePattern.Any).AfterGroup(null, suffix));

    public Part NamedAfterGroup<T>(string suffix = "") where T : Concept =>
        WithName((NamePattern ?? NamePattern.Any).AfterGroup(typeof(T), suffix));

    Part WithName(NamePattern name) {
        NamePattern = name;
        return this;
    }

    internal void ClearTypes() {
        types.Clear();
        requiredAtCallSite.Clear();
    }

    public bool? RequiredOf(Type type) => requiredAtCallSite.TryGetValue(type, out var value) ? value : options.Required;

    IPartChain IPartChain.Add<T>() => Add<T>();

    IPartChain IPartChain.InParentFolder() => InParentFolder();

    IPartChain IPartChain.OfType<T>(bool? required) => OfType<T>(required);

    IPartChain IPartChain.OfType(Type type, bool? required) => OfType(type, required);

    IPartChain IPartChain.NameStartsWith(string text) => NameStartsWith(text);

    IPartChain IPartChain.NameEndsWith(string text) => NameEndsWith(text);

    IPartChain IPartChain.NamedAfterGroup(string suffix) => NamedAfterGroup(suffix);

    IPartChain IPartChain.NamedAfterGroup<T>(string suffix) => NamedAfterGroup<T>(suffix);

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

    internal bool InFamily(Type concept) {
        for (var type = Concept?.GetType(); type is not null; type = type.DeclaringType) {
            if (type == concept) {
                return true;
            }
        }
        return false;
    }

    internal Part Group<T>(string? folder, Boundary? boundary) where T : Concept, new() {
        var concept = new T();
        var part = new Part(concept, folder ?? concept.GroupFolder ?? concept.Folder, this, group: true);
        part.options.Boundary = boundary;
        return Attach(part);
    }

    Part Attach(Part child) {
        RefuseInBlockPlace(child);
        Block(child);
        child.options = child.options.Over(Options.From(child.Concept!.ConfigureDefaults));
        children.Add(child);
        child.Concept!.DefinePart(child);
        return child;
    }

    void RefuseInBlockPlace(Part child) {
        if (IsBlockPlace) {
            throw new InvalidOperationException(
                $"Define: {child.Name} is under {Name}, but {Name} is the place of another block. The block in that place has its own definition. "
                + $"Remove the part, or add it in the definition of the block in {Path}.");
        }
        if (child.IsBlockPlace && (IsExternal || child.Concept is External)) {
            throw new InvalidOperationException(
                $"Define: the block place {child.Name} is an external or under an external. A block place is a folder. Add it outside the externals.");
        }
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
