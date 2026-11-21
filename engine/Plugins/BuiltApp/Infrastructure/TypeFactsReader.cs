namespace BuiltApp.Infrastructure;

using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using Umpire.Architecture;

static class TypeFactsReader {
    public static IReadOnlyList<TypeFacts> Read(
        IReadOnlyList<IType> types, IReadOnlyDictionary<string, Source> sources, ResolvedArchitecture architecture) {
        var loaded = types.ToHashSet();
        bool IsExternalPartType(IType type) =>
            loaded.Contains(type) && !LoadedTypes.IsCompilerGenerated(type.Name)
            && architecture.ExternalPart(AssemblyName(type), sources.ContainsKey(type.FullName) ? "" : type.FullName) is not null;
        return [.. types.Select(type => Facts(type, sources.GetValueOrDefault(type.FullName), loaded, IsExternalPartType))];
    }

    internal static string AssemblyName(IType type) => type.Assembly.Name.Split(',')[0];

    static TypeFacts Facts(IType type, Source? source, HashSet<IType> loaded, Func<IType, bool> isExternalPartType) => new(
        type.FullName,
        type.Name,
        AssemblyName(type),
        source,
        [.. Bases(type).Select(b => b.FullName)],
        [.. type.Dependencies.Select(d => d.Target).Distinct().Where(loaded.Contains).Select(t => t.FullName)],
        [.. Mentions(type).Where(loaded.Contains).Select(t => t.FullName).Distinct()],
        [.. Exposures.Of(type, isExternalPartType)
            .DistinctBy(e => new { e.Target, e.Member })
            .Where(e => loaded.Contains(e.Target))
            .Select(e => new TypeExposure(e.Target.FullName, e.Access, e.Member))]) {
        BaseArguments = [.. BaseArguments(type)],
        IsAbstract = type is Interface || type is Class { IsAbstract: true, IsSealed: not true },
        Kind = KindOf(type),
        Attributes = [.. type.Attributes.Concat(type.Members.SelectMany(m => m.Attributes)).Select(a => a.FullName).Distinct()],
    };

    static TypeKind KindOf(IType type) => type switch {
        Interface => TypeKind.Interface,
        ArchUnitNET.Domain.Enum => TypeKind.Enum,
        Struct => TypeKind.Struct,
        Class { BaseClass.FullName: "System.MulticastDelegate" } => TypeKind.Delegate,
        Class { IsAbstract: true, IsSealed: true } => TypeKind.StaticClass,
        Class { IsAbstract: true } => TypeKind.AbstractClass,
        Class { IsRecord: true } => TypeKind.Record,
        _ => TypeKind.Class,
    };

    static IEnumerable<IType> Bases(IType type) => new[] { type }
        .Concat(type is Class c ? c.InheritedClasses : [])
        .Concat(type.ImplementedInterfaces);

    static IEnumerable<string> BaseArguments(IType type) => new[] { type }
        .Concat(type is Class c ? c.InheritedClasses : [])
        .SelectMany(t => t.Dependencies)
        .Where(d => d is InheritsBaseClassDependency or ImplementsInterfaceDependency)
        .SelectMany(d => d.TargetGenericArguments)
        .Select(argument => argument.Type.FullName)
        .Distinct();

    static IEnumerable<IType> Mentions(IType type) =>
        type.Dependencies.SelectMany(d => new[] { d.Target }.Concat(d.TargetGenericArguments.Select(g => g.Type)));
}
