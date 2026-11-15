using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed class Bindings {
    readonly Dictionary<IType, Binding> byType;

    public Bindings(IReadOnlyList<Binding> all, IReadOnlyList<UnmappedType> unmapped) {
        All = all;
        Unmapped = unmapped;
        byType = all.ToDictionary(b => b.Type);
    }

    public IReadOnlyList<Binding> All { get; }
    public IReadOnlyList<UnmappedType> Unmapped { get; }
    public IReadOnlyList<IType> BoundTypes => [.. All.Where(b => !b.External).Select(b => b.Type)];
    public IReadOnlyList<IType> AppTypes => [.. BoundTypes.Concat(Unmapped.Select(u => u.Type))];

    public Binding? Of(IType type) => byType.GetValueOrDefault(type);
}
