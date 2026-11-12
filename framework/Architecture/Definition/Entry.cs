namespace VSA.Architecture;

internal sealed class Entry(Type layer, Reach? reach) {
    public Type Target { get; } = layer;
    public Reach? Reach { get; } = reach;
    public List<Filter> Filters { get; } = [];
    string Name => Reach is null ? Layer.Display(Target) : $"{Layer.Display(Target)}({Reach})";

    public override string ToString() => string.Join(" ", new[] { Name }.Concat(Filters.Select(f => f.ToString())));
}
