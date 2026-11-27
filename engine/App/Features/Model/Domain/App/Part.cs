namespace Umpire.CheckModel;

public sealed class Part {
    readonly PartDefinition definition;
    readonly List<Part> children = [];

    Part(PartDefinition definition, Part? parent, int position) {
        this.definition = definition;
        Parent = parent;
        Position = position;
        Path = PathOf(definition, parent);
        Ancestry = [this, .. parent?.Ancestry ?? []];
        Groups = [.. Ancestry.Where(p => p.IsGroup)];
    }

    public int Position { get; }
    public Part? Parent { get; }
    public IReadOnlyList<Part> Children => children;
    public IReadOnlyList<Part> Ancestry { get; }
    public IReadOnlyList<Part> Groups { get; }
    public string Path { get; }
    public string Name => definition.Concept.Display;
    public TypeName Concept => definition.Concept;
    public string BodyFolder => definition.BodyFolder;
    public string? External => definition.External;
    public bool IsExternal => External is not null;
    public bool HasFolder => definition.Folder is not null;
    public bool IsGroup => definition.IsGroup;
    public bool IsBlockPlace => definition.IsBlockPlace;
    public bool IsKernelRoot => definition.IsKernelRoot;
    public bool IsBlock => definition.IsBlock;
    public bool IsApp => definition.IsApp;
    public PartOptions Options => definition.Options;
    public IReadOnlyList<TypeName> Types => definition.Types;
    public NameRule? NameRule => definition.Name;
    public bool IsInParentFolder => !HasFolder && !IsGroup;
    public IReadOnlyList<Part> InThisFolder =>
        [.. children.Where(c => c.IsInParentFolder).SelectMany(c => new[] { c }.Concat(c.InThisFolder))];

    public static IReadOnlyList<Part> All(Definition definition) {
        var all = new List<Part>();
        foreach (var part in definition.Parts) {
            Add(part, null, all);
        }
        return all;
    }

    public bool Holds(Part other) => other.Ancestry.Contains(this);

    static void Add(PartDefinition definition, Part? parent, List<Part> all) {
        var part = new Part(definition, parent, all.Count);
        all.Add(part);
        parent?.children.Add(part);
        foreach (var child in definition.Children) {
            Add(child, part, all);
        }
    }

    static string PathOf(PartDefinition definition, Part? parent) {
        var own = definition.IsGroup
            ? $"{definition.Folder}/{{{definition.Concept.Display.ToLowerInvariant()}}}"
            : definition.Folder;
        if (parent is null || own is null) {
            return own ?? parent?.Path ?? "";
        }
        return parent.Path.Length == 0 ? own : $"{parent.Path}/{own}";
    }
}
