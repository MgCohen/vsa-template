namespace VSA.Architecture;

internal sealed class Entry(Type layer, Depth? depth, Access? access = null, Boundary? boundary = null) {
    public Type Target { get; } = layer;
    public Depth? Depth { get; } = depth;
    public Access? Access { get; } = access;
    public Boundary? Boundary { get; } = boundary;
    public List<Filter> Filters { get; } = [];
    string Name => Layer.Display(Target) + (new object?[] { Access, Depth, Boundary }.OfType<object>().ToList() is { Count: > 0 } options ? $"({string.Join(", ", options)})" : "");

    public override string ToString() => string.Join(" ", new[] { Name }.Concat(Filters.Select(f => f.ToString())));
}
