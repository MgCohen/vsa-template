namespace BuiltApp.Infrastructure;

using ArchUnitNET.Domain;
using Umpire.Architecture;

static class TypeFactsReader {
    public static IReadOnlyList<TypeFacts> Read(
        IReadOnlyList<IType> types, IReadOnlyDictionary<string, Source> sources, ResolvedArchitecture architecture) {
        var loaded = types.ToHashSet();
        bool IsFramework(IType type) =>
            loaded.Contains(type) && !type.Name.StartsWith('<') && architecture.ExternalPart(AssemblyName(type)) is not null;
        return [.. types.Select(type => Facts(type, sources.GetValueOrDefault(type.FullName), loaded, IsFramework))];
    }

    static TypeFacts Facts(IType type, Source? source, HashSet<IType> loaded, Func<IType, bool> isFramework) => new(
        type.FullName,
        type.Name,
        AssemblyName(type),
        source,
        [.. Bases(type).Select(b => b.FullName)],
        [.. type.Dependencies.Select(d => d.Target).Distinct().Where(loaded.Contains).Select(t => t.FullName)],
        [.. Mentions(type).Where(loaded.Contains).Select(t => t.FullName).Distinct()],
        [.. Exposures.Of(type, isFramework)
            .DistinctBy(e => new { e.Target, e.Member })
            .Where(e => loaded.Contains(e.Target))
            .Select(e => new TypeExposure(e.Target.FullName, e.Access, e.Member))]);

    static IEnumerable<IType> Bases(IType type) => new[] { type }
        .Concat(type is Class c ? c.InheritedClasses : [])
        .Concat(type.ImplementedInterfaces);

    static IEnumerable<IType> Mentions(IType type) =>
        type.Dependencies.SelectMany(d => new[] { d.Target }.Concat(d.TargetGenericArguments.Select(g => g.Type)));

    static string AssemblyName(IType type) => type.Assembly.Name.Split(',')[0];
}
