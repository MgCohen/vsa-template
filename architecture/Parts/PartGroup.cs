namespace VSA.Architecture;

public sealed class PartGroup : IPartChain {
    readonly List<Part> parts;

    internal PartGroup(IEnumerable<Part> parts) {
        this.parts = [.. parts];
    }

    public PartGroup Add<T>() where T : Concept, new() => new(parts.Select(p => p.Add<T>()));

    public PartGroup AddGroup<T>(string? folder = null) where T : Concept, new() => new(parts.Select(p => p.Group<T>(folder, null)));

    public PartGroup AddGroup<T>(Boundary boundary, string? folder = null) where T : Concept, new() =>
        new(parts.Select(p => p.Group<T>(folder, boundary)));

    public PartGroup And<T>() where T : Concept, new() {
        var parents = parts.Select(p => p.Parent ?? throw new InvalidOperationException("The root has no sibling.")).Distinct();
        return new(parts.Concat(parents.Select(p => p.Add<T>())));
    }

    public PartGroup InParentFolder() {
        foreach (var part in parts) {
            part.InParentFolder();
        }
        return this;
    }

    public PartGroup OfType<T>() => OfType(typeof(T));

    public PartGroup OfType(Type type) {
        foreach (var part in parts) {
            part.OfType(type);
        }
        return this;
    }

    IPartChain IPartChain.Add<T>() => Add<T>();

    IPartChain IPartChain.InParentFolder() => InParentFolder();

    IPartChain IPartChain.OfType<T>() => OfType<T>();

    IPartChain IPartChain.OfType(Type type) => OfType(type);
}
