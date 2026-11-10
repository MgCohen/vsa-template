using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Fluent.Slices;

namespace Structure;

public class Part : IObjectProvider<IType>
{
    readonly List<Part> children = [];
    readonly string[] segments;
    readonly Type? baseType;
    List<Part>? allowed;
    List<Part>? exposed;

    public Part(string folder) => segments = folder.Split('/', StringSplitOptions.RemoveEmptyEntries);

    public Part(Type baseType)
    {
        segments = [];
        this.baseType = baseType;
    }

    protected Part(string[] segments, string? itemName)
    {
        this.segments = segments;
        ItemName = itemName;
    }

    public Part? Parent { get; private set; }
    public IReadOnlyList<Part> Children => children;
    public string? ItemName { get; }
    public bool IsEach => ItemName is not null;
    public bool IsLayer => allowed is not null;
    public IReadOnlyList<Part> Allowed => allowed ?? [];
    public IReadOnlyList<Part>? Exposed => exposed;
    public int? ExactCount { get; private set; }

    public Part Add(Part child)
    {
        child.Parent = this;
        children.Add(child);
        return child;
    }

    public Part Each(string itemName) => Add(new Part(["*"], itemName));

    public Part MayUse(params Part[] parts)
    {
        (allowed ??= []).AddRange(parts);
        return this;
    }

    public Part ExposeOnly(params Part[] parts)
    {
        (exposed ??= []).AddRange(parts);
        return this;
    }

    public Part Exactly(int count)
    {
        ExactCount = count;
        return this;
    }

    public string Description => this is AppStructure ? "the app" : Path;

    public string Path
    {
        get
        {
            if (this is AppStructure) return "";
            var own = IsEach ? $"{{{ItemName}}}" : baseType is not null ? $"<{baseType.Name.Split('`')[0]}>" : string.Join('/', segments);
            var parent = Parent?.Path;
            return string.IsNullOrEmpty(parent) ? own : string.IsNullOrEmpty(own) ? parent : $"{parent}/{own}";
        }
    }

    public Part? EachLevel => IsEach ? this : Parent?.EachLevel;

    public IEnumerable<Part> SelfAndDescendants() => children.SelectMany(child => child.SelfAndDescendants()).Prepend(this);

    public IObjectProvider<IType> Own => new TypeSet($"{Path} (own)", types => types.Where(type => Root.LayerOf(type) == this));

    public IObjectProvider<IType> In(string item) =>
        new TypeSet($"{Path} of {item}", types => GetObjectsFrom(types).Where(type => Root.ItemOf(type, EachLevel!) == item));

    public SliceAssignment Slices => By(EachLevel ?? throw new InvalidOperationException($"{Path} has no Each level above it."));

    public SliceAssignment By(Part each)
    {
        if (!each.IsEach || !SelfAndAncestors().Contains(each))
            throw new InvalidOperationException($"{Path} can be cut only by an Each level above it. {each.Path} is not one.");
        return new SliceAssignment(type => Contains(type) && Root.ItemOf(type, each) is not null
            ? SliceIdentifier.Of(each.SelfAndAncestors().Where(level => level.IsEach)
                .Aggregate(Path, (path, level) => path.Replace($"{{{level.ItemName}}}", Root.ItemOf(type, level))))
            : SliceIdentifier.Ignore(), $"of {Path}, one for each {each.ItemName},");
    }

    public IEnumerable<IType> GetObjects(Architecture architecture) => GetObjectsFrom(architecture.Types);

    public string FormatDescription(string emptyDescription, string singleDescription, string multipleDescription) =>
        $"{multipleDescription} in {Description}";

    public override string ToString() => Path;

    internal IEnumerable<IType> GetObjectsFrom(IEnumerable<IType> types) => types.Where(Contains);

    internal bool Contains(IType type)
    {
        var folder = Root.Map.FolderOf(type.FullName);
        if (folder is null || !FolderMatches(folder.Split('/', StringSplitOptions.RemoveEmptyEntries))) return false;
        return SelfAndAncestors().All(part => part.baseType is null || IsAssignableTo(type, part.baseType));
    }

    internal int Depth => Parent is null ? 0 : Parent.Depth + 1;

    internal string[] FullSegments => [.. Parent?.FullSegments ?? [], .. segments];

    internal IEnumerable<Part> SelfAndAncestors() => Parent is null ? [this] : Parent.SelfAndAncestors().Prepend(this);

    AppStructure Root => this as AppStructure ?? Parent?.Root ?? throw new InvalidOperationException($"{Path} is not in a structure.");

    bool FolderMatches(string[] folder)
    {
        var pattern = FullSegments;
        if (folder.Length < pattern.Length) return false;
        return pattern.Select((segment, index) => segment == "*" || segment == folder[index]).All(match => match);
    }

    static bool IsAssignableTo(IType type, Type baseType)
    {
        var name = baseType.FullName!;
        var seen = new HashSet<IType>();
        var stack = new Stack<IType>([type]);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current)) continue;
            if (current.FullName == name || current.FullName.StartsWith(name + "<") || current.FullName.StartsWith(name + "[")) return true;
            foreach (var dependency in current.Dependencies)
                if (dependency is InheritsBaseClassDependency or ImplementsInterfaceDependency) stack.Push(dependency.Target);
        }
        return false;
    }
}

sealed class TypeSet(string description, Func<IEnumerable<IType>, IEnumerable<IType>> select) : IObjectProvider<IType>
{
    public string Description => description;
    public IEnumerable<IType> GetObjects(Architecture architecture) => select(architecture.Types);
    public string FormatDescription(string emptyDescription, string singleDescription, string multipleDescription) =>
        $"{multipleDescription} in {description}";
}
