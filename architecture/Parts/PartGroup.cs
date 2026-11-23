namespace Umpire.Architecture;

public sealed class PartGroup : IPartChain {
    readonly List<Part> parts;
    readonly bool overlay;

    internal PartGroup(IEnumerable<Part> parts, bool overlay = false) {
        this.parts = [.. parts];
        this.overlay = overlay;
    }

    public PartGroup Add<T>() where T : Concept, new() => new(parts.Select(p => In(p, part => part.Add<T>())), overlay);

    public PartGroup AddGroup<T>(string? folder = null) where T : Concept, new() =>
        new(parts.Select(p => In(p, part => part.Group<T>(folder, null))), overlay);

    public PartGroup AddGroup<T>(Boundary boundary, string? folder = null) where T : Concept, new() =>
        new(parts.Select(p => In(p, part => part.Group<T>(folder, boundary))), overlay);

    public PartGroup And<T>() where T : Concept, new() {
        var parents = parts.Select(p => p.Parent ?? throw new InvalidOperationException("The block has no sibling.")).Distinct();
        return new(parts.Concat(parents.Select(p => In(p, part => part.Add<T>()))), overlay);
    }

    Part In(Part parent, Func<Part, Part> add) => overlay ? parent.Overlay(add) : add(parent);

    public PartGroup InParentFolder() {
        foreach (var part in parts) {
            part.InParentFolder();
        }
        return this;
    }

    public PartGroup OfType<T>(bool? required = null) => OfType(typeof(T), required);

    public PartGroup OfType(Type type, bool? required = null) {
        foreach (var part in parts) {
            part.OfType(type, required);
        }
        return this;
    }

    public PartGroup NameStartsWith(string text) => Each(part => part.NameStartsWith(text));

    public PartGroup NameEndsWith(string text) => Each(part => part.NameEndsWith(text));

    public PartGroup NamedAfterGroup(string suffix = "") => Each(part => part.NamedAfterGroup(suffix));

    public PartGroup NamedAfterGroup<T>(string suffix = "") where T : Concept => Each(part => part.NamedAfterGroup<T>(suffix));

    PartGroup Each(Action<Part> name) {
        foreach (var part in parts) {
            name(part);
        }
        return this;
    }

    IPartChain IPartChain.Add<T>() => Add<T>();

    IPartChain IPartChain.NameStartsWith(string text) => NameStartsWith(text);

    IPartChain IPartChain.NameEndsWith(string text) => NameEndsWith(text);

    IPartChain IPartChain.NamedAfterGroup(string suffix) => NamedAfterGroup(suffix);

    IPartChain IPartChain.NamedAfterGroup<T>(string suffix) => NamedAfterGroup<T>(suffix);

    IPartChain IPartChain.InParentFolder() => InParentFolder();

    IPartChain IPartChain.OfType<T>(bool? required) => OfType<T>(required);

    IPartChain IPartChain.OfType(Type type, bool? required) => OfType(type, required);
}
