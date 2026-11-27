namespace Facts.Infrastructure;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;
using Umpire.Architecture;

sealed class DllTypeReader {
    const string ObjectType = "System.Object";
    const string Delegate = "System.MulticastDelegate";
    const string CloneMethod = "<Clone>$";
    const string PrintMembers = "PrintMembers";
    const string RefLike = "System.Runtime.CompilerServices.IsByRefLikeAttribute";
    static readonly string[] PlainBases = [ObjectType, "System.ValueType", "System.Enum", Delegate, "System.Delegate"];

    readonly Type type;
    readonly DllContext context;
    readonly Source source;
    readonly TypeName name;
    readonly UseList uses;
    readonly DllMemberReader members;

    public DllTypeReader(Type type, DllContext context, Source source) {
        this.type = type;
        this.context = context;
        this.source = source;
        name = ReflectionShapes.NameOf(type);
        uses = new UseList(name, source);
        members = new DllMemberReader(type, context, source.Line);
    }

    public DllType Read() {
        var all = ReflectionFlags.Safe<Type[]?>(() => type.GetInterfaces(), null);
        var chain = Chain();
        var lost = chain.Count == 0 && !type.IsInterface ? MissingBase() : null;
        var kind = KindOf([.. chain.Select(ReflectionShapes.NameOf), .. lost is null ? [] : new[] { lost }]);
        var declared = DeclaredInterfaces.Names(type, context);
        var allShapes = all?.Select(ReflectionShapes.Of).ToList() ?? [.. declared.Select(Shape)];
        var interfaces = all?.Where(i => DeclaredInterfaces.IsDeclared(i, declared)).Select(ReflectionShapes.Of).ToList() ?? allShapes;
        var baseType = chain.Count > 0 ? chain[0] : null;
        var baseShape = baseType is null || PlainBases.Contains(baseType.FullName) ? null : ReflectionShapes.Of(baseType);
        baseShape ??= lost is null || PlainBases.Contains(lost.FullName) ? null : new DllShape(lost.FullName, [lost]);
        var attributes = Attributes().ToList();
        var read = members.Read().ToList();
        TypeUses(baseShape, interfaces, attributes, read);
        BodyUses();
        return new DllType(
            name, type.Namespace ?? "", type.IsNested && type.DeclaringType is { } outer ? ReflectionShapes.NameOf(outer).FullName : null,
            source, kind, ReflectionAccess.Of(type), Traits(kind), baseShape) {
            TypeParameters = [.. OwnTypeParameters().Select(DllTypeParameters.Of)],
            Interfaces = interfaces,
            AllInterfaces = allShapes,
            Bases = [.. Bases(kind, chain, lost, allShapes)],
            BaseArguments = [.. chain.Concat(all ?? []).SelectMany(ReflectionShapes.Arguments)
                .SelectMany(a => ReflectionShapes.Named(a).Take(1)).Select(ReflectionShapes.NameOf).Distinct()],
            Attributes = attributes,
            Members = read,
            MutableMembers = [.. members.Mutable(kind == DllTypeKind.Enum)],
            Uses = uses.All,
        };
    }

    List<Type> Chain() {
        var found = new List<Type>();
        try {
            for (var current = type.BaseType; current is not null && current.FullName != ObjectType; current = current.BaseType) {
                found.Add(current);
            }
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return found;
        }
        return found;
    }

    TypeName? MissingBase() {
        var baseHandle = context.Symbols.Metadata.GetTypeDefinition(Handle).BaseType;
        return baseHandle.IsNil ? null : context.Names.Head(baseHandle) is { FullName: not ObjectType } found ? found : null;
    }

    static DllTypeKind KindOf(IReadOnlyList<TypeName> chain, bool isInterface) =>
        isInterface ? DllTypeKind.Interface
        : chain.Any(t => t.FullName == "System.Enum") ? DllTypeKind.Enum
        : chain.Count > 0 && chain[0].FullName == Delegate ? DllTypeKind.Delegate
        : chain.Any(t => t.FullName == "System.ValueType") ? DllTypeKind.Struct
        : DllTypeKind.Class;

    DllTypeKind KindOf(IReadOnlyList<TypeName> chain) => KindOf(chain, type.IsInterface);

    static DllShape Shape(IReadOnlyList<TypeName> names) => new(names[0].FullName, names);

    DllTypeTraits Traits(DllTypeKind kind) {
        var methods = members.Methods().Select(m => m.Name).ToList();
        var hidden = ReflectionFlags.Safe(() => type.GetMethods(ReflectionFlags.Declared).Select(m => m.Name).ToList(), []);
        var isRecord = kind == DllTypeKind.Class ? hidden.Contains(CloneMethod) : kind == DllTypeKind.Struct && methods.Contains(PrintMembers);
        return new DllTypeTraits(
            type.IsAbstract && !type.IsSealed, kind == DllTypeKind.Class && type.IsAbstract && type.IsSealed, type.IsSealed && !type.IsAbstract,
            isRecord, ReflectionFlags.IsReadOnly(type), ReflectionFlags.Has(type, RefLike));
    }

    IEnumerable<TypeName> Bases(DllTypeKind kind, List<Type> chain, TypeName? lost, List<DllShape> all) {
        var root = kind is DllTypeKind.Class or DllTypeKind.Delegate && lost is null && ReflectionFlags.Safe(() => ObjectOf(chain), null) is { } found
            ? new[] { ReflectionShapes.NameOf(found) }
            : [];
        return new[] { type }.Concat(chain).Select(ReflectionShapes.NameOf)
            .Concat(lost is null ? [] : new[] { lost })
            .Concat(root)
            .Concat(all.Where(i => i.Types.Count > 0).Select(i => i.Types[0]))
            .Distinct();
    }

    Type? ObjectOf(List<Type> chain) => (chain.Count > 0 ? chain[chain.Count - 1] : type).BaseType;

    TypeDefinitionHandle Handle => MetadataTokens.TypeDefinitionHandle(type.MetadataToken);

    IEnumerable<Type> OwnTypeParameters() {
        if (!type.IsGenericTypeDefinition) {
            return [];
        }
        var outer = type.DeclaringType is { IsGenericTypeDefinition: true } declaring ? declaring.GetGenericArguments().Length : 0;
        return type.GetGenericArguments().Skip(outer);
    }

    IEnumerable<DllAttributeFact> Attributes() {
        var accessors = new HashSet<int>(members.Accessors.Select(a => a.MetadataToken));
        var own = ReflectionAttributes.Of(ReflectionAttributes.On(type).Concat(OwnTypeParameters().SelectMany(ReflectionAttributes.On)), null);
        var ofMembers = members.Fields().Cast<MemberInfo>()
            .Concat(ReflectionFlags.Safe(() => type.GetProperties(ReflectionFlags.Declared), []))
            .Concat(ReflectionFlags.Safe(() => type.GetEvents(ReflectionFlags.Declared), []))
            .Concat(ReflectionFlags.Safe(() => type.GetConstructors(ReflectionFlags.Declared), []))
            .Concat(members.Methods().Where(m => !accessors.Contains(m.MetadataToken)))
            .SelectMany(m => ReflectionAttributes.Of(ReflectionAttributes.On(m), m.Name));
        return own.Concat(ofMembers);
    }

    void TypeUses(DllShape? baseShape, List<DllShape> interfaces, List<DllAttributeFact> attributes, List<DllMember> read) {
        var line = source.Line;
        if (baseShape is not null) {
            Add(baseShape, DllUseKind.Base, line, null);
        }
        foreach (var declared in interfaces) {
            Add(declared, DllUseKind.Interface, line, null);
        }
        foreach (var parameter in OwnTypeParameters().Select(DllTypeParameters.Of)) {
            uses.Add(parameter.Types.SelectMany(t => t.Types), DllUseKind.Constraint, line, null);
        }
        uses.Add(attributes.Select(a => a.Type), DllUseKind.Attribute, line, null);
        foreach (var member in read.Where(m => !m.Traits.IsImplicit)) {
            var shapes = new[] { member.Type }.OfType<DllShape>().Concat(member.Parameters.Select(p => p.Type))
                .Concat(member.TypeParameters.SelectMany(p => p.Types));
            uses.Add(shapes.SelectMany(s => s.Types), DllUseKind.Signature, member.Line, member.Name);
        }
    }

    void Add(DllShape shape, DllUseKind kind, int line, string? member) {
        uses.Add(shape.Types.Take(1), kind, line, member);
        uses.Add(shape.Types.Skip(1), DllUseKind.TypeArgument, line, member);
    }

    void BodyUses() {
        new DllBodyWalk(context, uses, members.Accessors).Walk(type);
    }
}
