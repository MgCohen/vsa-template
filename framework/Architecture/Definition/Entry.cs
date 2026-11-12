namespace VSA.Architecture;

internal sealed class Entry(Type layer, Reach? reach, Access? access = null, Isolation? isolation = null) {
    public Type Target { get; } = layer;
    public Reach? Reach { get; } = reach;
    public Access? Access { get; } = access;
    public Isolation? Isolation { get; } = isolation;
    public List<Filter> Filters { get; } = [];
    string Name => Layer.Display(Target) + (new object?[] { Access, Reach, Isolation }.OfType<object>().ToList() is { Count: > 0 } options ? $"({string.Join(", ", options)})" : "");

    public override string ToString() => string.Join(" ", new[] { Name }.Concat(Filters.Select(f => f.ToString())));
}
