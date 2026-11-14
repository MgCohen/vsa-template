namespace VSA.Architecture;

internal sealed class Entry(Type concept, Depth? depth, Access? access = null, Boundary? boundary = null) {
    public Type Target { get; } = concept;
    public Depth? Depth { get; } = depth;
    public Access? Access { get; } = access;
    public Boundary? Boundary { get; } = boundary;
    public List<Filter> Filters { get; } = [];
    string Name => Concept.Display(Target) + (new object?[] { Access, Depth, Boundary }.OfType<object>().ToList() is { Count: > 0 } options ? $"({string.Join(", ", options)})" : "");

    public override string ToString() => string.Join(" ", new[] { Name }.Concat(Filters.Select(f => f.ToString())));
}
