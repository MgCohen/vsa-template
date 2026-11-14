using ArchUnitNET.Domain;

namespace VSA.Architecture.Engine;

public sealed class Bindings {
    readonly Dictionary<IType, Binding> byType;
    readonly HashSet<IType> unmappedTypes;

    public Bindings(IReadOnlyList<Binding> all, IReadOnlyList<UnmappedType> unmapped) {
        All = all;
        Unmapped = unmapped;
        byType = all.ToDictionary(b => b.Type);
        unmappedTypes = [.. unmapped.Select(u => u.Type)];
        Framework = all.Where(b => b.External).Select(b => b.Type).ToHashSet();
    }

    public IReadOnlyList<Binding> All { get; }
    public IReadOnlyList<UnmappedType> Unmapped { get; }
    public IReadOnlySet<IType> Framework { get; }
    public IReadOnlyList<IType> BoundTypes => [.. All.Where(b => !b.External).Select(b => b.Type)];
    public IReadOnlyList<IType> AppTypes => [.. BoundTypes.Concat(Unmapped.Select(u => u.Type))];

    public Binding? Of(IType type) => byType.GetValueOrDefault(type);

    public bool IsUnmapped(IType type) => unmappedTypes.Contains(type);

    public Source? SourceOf(IType type) => Of(type)?.Source ?? Unmapped.FirstOrDefault(u => u.Type.Equals(type))?.Source;
}
