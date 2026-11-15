namespace Umpire.Architecture;

public sealed class Bindings {
    readonly Dictionary<string, Binding> byName;

    public Bindings(IReadOnlyList<Binding> all, IReadOnlyList<UnmappedType> unmapped) {
        All = all;
        Unmapped = unmapped;
        byName = all.ToDictionary(b => b.Type.FullName);
    }

    public IReadOnlyList<Binding> All { get; }
    public IReadOnlyList<UnmappedType> Unmapped { get; }
    public IReadOnlyList<TypeFacts> BoundTypes => [.. All.Where(b => !b.External).Select(b => b.Type)];
    public IReadOnlyList<TypeFacts> AppTypes => [.. BoundTypes.Concat(Unmapped.Select(u => u.Type))];

    public Binding? Of(string fullName) => byName.TryGetValue(fullName, out var binding) ? binding : null;
}
