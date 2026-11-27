namespace Facts.Domain.Shared;

public sealed record AppClass(
    TypeName Type, string Source, IReadOnlyList<TypeName> Kernels, IReadOnlyList<TypeName> KernelBases,
    IReadOnlyList<string> Bases) {
    public const string EntryInterface = "Umpire.Kernels.IAppEntry`1";
    const char Nested = '+';

    public bool IsOpen { get; init; }

    public bool HasManyKernels => Kernels.Count > 1;

    public IReadOnlyList<TypeName> KernelChain => [Kernels[0], .. KernelBases];

    public static IReadOnlyList<Type> KernelsOf(Type type) {
        if (type is not { IsAbstract: false, IsInterface: false }) {
            return [];
        }
        return [.. type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == EntryInterface)
            .Select(i => i.GetGenericArguments()[0])
            .Where(kernel => !kernel.ContainsGenericParameters)
            .OrderBy(kernel => kernel.FullName, StringComparer.Ordinal)];
    }

    public static IReadOnlyList<AppClass> Leaves(IReadOnlyList<AppClass> classes) {
        var leaves = classes
            .Where(c => !classes.Any(other => other.Bases.Contains(c.Type.FullName) || other.Holds(c)))
            .ToList();
        return leaves.Any(c => !c.IsOpen) ? [.. leaves.Where(c => !c.IsOpen)] : leaves;
    }

    bool Holds(AppClass inner) =>
        inner.Type.FullName.StartsWith(Type.FullName + Nested, StringComparison.Ordinal)
        && inner.Kernels.SequenceEqual(Kernels);
}
