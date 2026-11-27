namespace Facts.Infrastructure;

using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using Umpire.Architecture;
using Umpire.Utility;

static class TypeFactsReader {
    const string PrintMembers = "PrintMembers(System.Text.StringBuilder)";

    public static IReadOnlyList<TypeFacts> Read(
        IReadOnlyList<IType> types, IReadOnlyDictionary<string, Source> sources, ResolvedArchitecture architecture,
        ConstantFields constants) {
        var loaded = types.ToHashSet();
        bool IsExternalPartType(IType type) =>
            loaded.Contains(type) && !LoadedTypes.IsCompilerGenerated(type.Name)
            && architecture.ExternalPart(AssemblyName(type), sources.ContainsKey(type.FullName) ? "" : type.FullName) is not null;
        return [.. types.Select(type => Facts(type, sources.GetValueOrDefault(type.FullName), loaded, IsExternalPartType, constants))];
    }

    internal static string AssemblyName(IType type) => type.Assembly.Name.Split(',')[0];

    static TypeFacts Facts(IType type, Source? source, HashSet<IType> loaded, Func<IType, bool> isExternalPartType,
        ConstantFields constants) => new(
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
        IsStatic = type is Class { IsAbstract: true, IsSealed: true },
        IsSealed = type is Class { IsSealed: true, IsAbstract: not true } && !IsDelegate(type),
        IsRecord = type is Class { IsRecord: true } || (type is Struct && type.Members.Any(m => m.Name == PrintMembers)),
        IsStruct = type is Struct,
        IsInterface = type is Interface,
        IsEnum = type is ArchUnitNET.Domain.Enum,
        IsDelegate = IsDelegate(type),
        Attributes = [.. type.Attributes.Concat(type.Members.SelectMany(m => m.Attributes)).SelectMany(AttributeNames).Distinct()],
        MutableMembers = [.. MutableMembers(type, constants).Distinct()],
        References = [.. Referenced(type).Where(t => t != type).Select(t => t.FullName).Distinct()],
        IsNested = type.IsNested,
    };

    static IEnumerable<string> MutableMembers(IType type, ConstantFields constants) => type is ArchUnitNET.Domain.Enum ? [] : type.Members
        .Where(m => m switch {
            FieldMember field => field.Writability == Writability.Writable && !LoadedTypes.IsCompilerGenerated(field.Name)
                && !constants.Contains(type.FullName, field.Name),
            PropertyMember property => property.Writability == Writability.Writable,
            _ => false,
        })
        .Select(m => m.Name);

    static IEnumerable<string> AttributeNames(ArchUnitNET.Domain.Attribute attribute) =>
        new[] { attribute.FullName }.Concat(attribute.InheritedClasses.Select(c => c.FullName));

    static bool IsDelegate(IType type) => type is Class { BaseClass.FullName: "System.MulticastDelegate" };

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

    static IEnumerable<IType> Referenced(IType type) => type.Dependencies
        .Where(d => d is not IMemberTypeDependency { OriginMember: var member } || !IsCompilerMade(member))
        .SelectMany(d => new[] { d.Target }.Concat(d.TargetGenericArguments.Select(g => g.Type)));

    static bool IsCompilerMade(IMember member) => member.IsCompilerGenerated
        || member.Attributes.Any(a => a.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    static IEnumerable<IType> Mentions(IType type) =>
        type.Dependencies.SelectMany(d => new[] { d.Target }.Concat(d.TargetGenericArguments.Select(g => g.Type)));
}
