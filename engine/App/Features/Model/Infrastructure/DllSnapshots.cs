namespace Model.Infrastructure;

using Facts.Domain.Shared.Dll;
using Umpire.CheckModel;
using DllName = Facts.Domain.Shared.TypeName;

public static class DllSnapshots {
    public static IReadOnlyList<Snapshot> Of(IReadOnlyList<DllFacts> dlls, string block) {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var snapshots = new List<Snapshot>();
        foreach (var dll in dlls) {
            var types = dll.Types.Where(t => Path.IsPathRooted(t.Source.File) && seen.Add(t.Name.FullName)).ToList();
            snapshots.Add(Of(dll, types, block));
        }
        return snapshots;
    }

    static Snapshot Of(DllFacts dll, IReadOnlyList<DllType> types, string block) {
        var hashes = dll.Files.Where(f => Path.IsPathRooted(f.Path)).GroupBy(f => BlockPath.Full(f.Path), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Hash, StringComparer.OrdinalIgnoreCase);
        var entries = types.SelectMany(Entries).ToLookup(e => BlockPath.Full(e.File), StringComparer.OrdinalIgnoreCase);
        var paths = hashes.Keys.Concat(entries.Select(e => e.Key)).Distinct(StringComparer.OrdinalIgnoreCase);
        var files = paths.Select(file => new FileFacts(
            BlockPath.Of(block, file), hashes.TryGetValue(file, out var hash) ? hash : "", 0, false, [.. entries[file].Select(e => e.Facts)]));
        return Snapshot.Of(Path.GetFileNameWithoutExtension(dll.Path), files);
    }

    static IEnumerable<Entry> Entries(DllType type) {
        var name = Name(type.Name);
        var content = Content(type);
        var primary = BlockPath.Full(type.Source.File);
        var byFile = type.Uses.GroupBy(u => Path.IsPathRooted(u.File) ? BlockPath.Full(u.File) : primary, StringComparer.OrdinalIgnoreCase)
            .ToList();
        yield return new Entry(type.Source.File, new TypeFacts(
            name, new TypeLocation(type.Namespace, type.Source.Line, type.Source.Column, type.Outer), content,
            [.. byFile.Where(g => string.Equals(g.Key, primary, StringComparison.OrdinalIgnoreCase)).SelectMany(g => g).Select(Reference)]));
        foreach (var other in byFile.Where(g => !string.Equals(g.Key, primary, StringComparison.OrdinalIgnoreCase))) {
            var first = other.OrderBy(u => u.Line).First();
            yield return new Entry(other.Key, new TypeFacts(
                name, new TypeLocation(type.Namespace, first.Line, first.Column, type.Outer),
                content with { Members = [], Attributes = [] }, [.. other.Select(Reference)]));
        }
    }

    static TypeContent Content(DllType type) => new(
        Parsed<TypeKind>(type.Kind), Access(type.Access), type.Traits.IsAbstract, type.Traits.IsStatic, type.Traits.IsSealed,
        type.Traits.IsRecord, false, type.Traits.IsReadOnly, type.Traits.IsRefLike,
        [.. type.TypeParameters.Select(Parameter)],
        [.. type.Interfaces.Select(Shape)],
        [.. type.AllInterfaces.Select(Shape)],
        [.. type.Bases.Select(Name)],
        [.. type.BaseArguments.Select(Name)],
        [.. type.Attributes.Select(a => new AttributeFact(Name(a.Type), [.. a.Bases.Select(Name)], a.Arguments, a.Member))],
        [.. type.Members.Select(Member)],
        type.MutableMembers,
        type.Base is null ? null : Shape(type.Base));

    static MemberFact Member(DllMember member) => new(
        member.Name, Parsed<MemberKind>(member.Kind), Access(member.Access),
        [.. member.Parameters.Select(p => new ParameterFact(p.Name, Shape(p.Type), p.IsOptional, p.IsParams, p.Passing))],
        [.. member.TypeParameters.Select(Parameter)],
        [.. member.Implements.Select(Name)],
        member.Traits.IsStatic, member.Traits.IsAbstract, member.Traits.IsVirtual, member.Traits.IsOverride, member.Traits.IsSealed,
        member.Traits.IsReadOnly, member.Traits.IsConst, member.Traits.IsAsync, member.Traits.IsImplicit, member.Line,
        member.Type is null ? null : Shape(member.Type),
        member.Get is { } get ? Access(get) : null,
        member.Set is { } set ? Access(set) : null);

    static ReferenceFact Reference(DllUse use) =>
        new(Name(use.Target), Parsed<ReferenceKind>(use.Kind), use.Line, use.Column, use.Member, use.TargetMember);

    static TypeParameterFact Parameter(DllTypeParameter parameter) =>
        new(parameter.Name, parameter.Limits, [.. parameter.Types.Select(Shape)]);

    static TypeShape Shape(DllShape shape) => new(shape.Text, [.. shape.Types.Select(Name)]);

    static TypeName Name(DllName name) => new(name.FullName, name.Assembly);

    static MemberAccess Access(DllAccess access) => Parsed<MemberAccess>(access);

    static T Parsed<T>(Enum value) where T : struct => (T)Enum.Parse(typeof(T), value.ToString());

    sealed record Entry(string File, TypeFacts Facts);
}
