namespace VSA.Architecture;

public sealed class PartGroup {
    readonly List<Part> parts;

    internal PartGroup(IEnumerable<Part> parts) {
        this.parts = [.. parts];
    }

    public PartGroup Add<T>() where T : Layer, new() => new(parts.Select(p => p.Add<T>()));

    public PartGroup AddMany<T>(string? folder = null) where T : Layer, new() => new(parts.Select(p => p.AddMany<T>(folder)));

    public PartGroup And<T>() where T : Layer, new() {
        var parents = parts.Select(p => p.Parent ?? throw new InvalidOperationException("The root has no sibling.")).Distinct();
        return new(parts.Concat(parents.Select(p => p.Add<T>())));
    }

    public PartGroup NoFolder() {
        foreach (var part in parts) {
            part.NoFolder();
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
}
