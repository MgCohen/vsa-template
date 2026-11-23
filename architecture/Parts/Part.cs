namespace Umpire.Architecture;

using Umpire.Architecture.Blocks;

public class Part : IPartChain {
    readonly List<Part> children = [];
    readonly List<Type> types = [];
    readonly Dictionary<Type, bool> requiredAtCallSite = [];
    readonly HashSet<Type> stacked = [];
    PartOptions options = new();
    string? folder;
    bool open;
    bool external;

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
    internal string? Folder => folder;
    public string? Assembly => Concept is External library ? library.Assembly : null;
    public bool IsExternal => Assembly is not null;
    public IReadOnlyList<Part> Children => children;
    public IReadOnlyList<Type> Types => types;
    public string Name => Concept is null ? "(root)" : Concept.Display(Concept.GetType());
    public PartOptions Options => options.Over(new PartOptions());
    public IEnumerable<Part> Groups => SelfAndAncestors().Where(p => p.IsGroup);
    internal NamePattern? NamePattern { get; private set; }
    internal IReadOnlyList<Type> Placers { get; set; } = [];
    internal List<Extension> Extensions { get; } = [];
    internal bool IsKernelRoot { get; private set; }

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
        var part = new Part(concept, concept.Folder, this);
        RefuseInBlockPlace(part);
        if (children.FirstOrDefault(c => c.Concept!.GetType() == typeof(T) && !c.IsGroup && !c.IsBlockPlace) is { } added) {
            return added;
        }
        return Attach(part);
    }

    public PartGroup AddGroup<T>(string? folder = null) where T : Concept, new() => new([Group<T>(folder, null)]);

    public PartFind For<T>() where T : Concept => new(this, typeof(T));

    public PartGroup AddGroup<T>(Boundary boundary, string? folder = null) where T : Concept, new() => new([Group<T>(folder, boundary)]);

    public PartGroup And<T>() where T : Concept, new() => new PartGroup([this]).And<T>();

    public Part AddBlock<T>(string? folder = null) where T : Concept, new() {
        var concept = new T();
        var part = new Part(concept, folder ?? concept.Folder, this, blockPlace: true);
        RefuseInBlockPlace(part);
        if (children.FirstOrDefault(c => c.Concept!.GetType() == typeof(T) && !c.IsGroup && c.IsBlockPlace && c.folder == part.folder) is { } placed) {
            return placed;
        }
        return Attach(part);
    }

    internal Part AddOf(Type concept, string? folder) {
        var record = (Concept)Activator.CreateInstance(concept)!;
        return Attach(new Part(record, folder ?? record.Folder, this));
    }

    internal Part AddRoot(Concept record) =>
        Attach(new Part(record, record.Folder.Length == 0 ? null : record.Folder, this,
            blockPlace: record is Block and not App and not Definition));

    internal Part AddKernelRoot() {
        var kernel = Attach(new Part(new Kernel(), "Kernel", this));
        kernel.IsKernelRoot = true;
        return kernel;
    }

    internal void Remove(Part child) => children.Remove(child);

    internal void AddExtension(Extension extension) => SelfAndAncestors().Last().Extensions.Add(extension);

    internal T Overlay<T>(Func<Part, T> add) {
        var wasOpen = open;
        open = true;
        try {
            return add(this);
        } finally {
            open = wasOpen;
        }
    }

    internal void StackBody(Block body) {
        if (stacked.Contains(body.GetType())) {
            return;
        }
        stacked.Add(body.GetType());
        var placers = Placers;
        Placers = Owners.Home(body) is { Count: > 0 } home ? home : Placers;
        try {
            Overlay(part => {
                body.DefinePart(part);
                return part;
            });
        } finally {
            Placers = placers;
        }
    }

    public Part AddBlockGroup<T>(string? folder = null) where T : Concept, new() {
        var concept = new T();
        return Attach(new Part(concept, folder ?? concept.GroupFolder ?? concept.Folder, this, group: true, blockPlace: true));
    }

    public Part InParentFolder() {
        folder = null;
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

    public bool Is(Type concept) => Concept is not null && concept.IsInstanceOfType(Concept);

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

    internal Part AddExternal(External library) {
        external = true;
        try {
            return Attach(new Part(library, library.Folder, this));
        } finally {
            external = false;
        }
    }

    Part Attach(Part child) {
        RefuseInBlockPlace(child);
        RefuseExternal(child);
        var owners = Owners.Of(child.Concept!);
        RefuseOwned(child, owners);
        child.Placers = child.Concept is Block && Owners.Home(child.Concept) is { Count: > 0 } home ? home : Placers;
        child.options = child.options.Over(PartOptions.From(child.Concept!.ConfigureDefaults));
        children.Add(child);
        foreach (var type in child.Concept.OfTypes) {
            child.OfType(type);
        }
        if (child.Concept.NameStartsWith.Length > 0) {
            child.NameStartsWith(child.Concept.NameStartsWith);
        }
        if (child.Concept.NameEndsWith.Length > 0) {
            child.NameEndsWith(child.Concept.NameEndsWith);
        }
        if (child.Concept is Block block) {
            RefuseLoop(child);
            child.open = true;
            block.DefinePart(child);
            child.open = false;
        }
        return child;
    }

    void RefuseLoop(Part child) {
        var type = child.Concept!.GetType();
        if (SelfAndAncestors().FirstOrDefault(p => p.Concept?.GetType() == type) is { } outer) {
            throw new InvalidOperationException(
                $"Define: {child.Name} is placed inside {outer.Name}, so the block places itself. "
                + $"Remove the place of {child.Name} from its own body, or from the block that places it.");
        }
    }

    void RefuseOwned(Part child, IReadOnlyList<Type> owners) {
        if (Owners.MayPlace(owners, Placers)) {
            return;
        }
        var owner = string.Join(" or ", owners.Select(o => o.Name));
        var placer = string.Join(", ", Placers.Select(p => p.Name));
        throw new InvalidOperationException(
            $"Define: {placer} adds {child.Name}, but {child.Name} belongs to {owner}. "
            + $"Only {owner}, or an architecture that derives from it, can place {child.Name}. "
            + $"Add your own concept, place a block that holds {child.Name}, or give {child.Name} another OwnedBy.");
    }

    void RefuseInBlockPlace(Part child) {
        if (IsBlockPlace && !open) {
            throw new InvalidOperationException(
                $"Define: {child.Name} is under {Name}, but {Name} is the place of another block. The block in that place has its own definition. "
                + $"Remove the part, or add it in the definition of the block in {Path}.");
        }
    }

    void RefuseExternal(Part child) {
        if (child.Concept is not External || external) {
            return;
        }
        var call = child.IsGroup ? $"AddGroup<{child.Name}>()" : $"Add<{child.Name}>()";
        throw new InvalidOperationException(
            $"Define: {call} adds an External. An External is a DLL, not a folder, so it has no place in Define. "
            + $"Name it in a rule, for example rules.For<Infrastructure>().Uses<{child.Name}>(). The rule gives it its part.");
    }
}
