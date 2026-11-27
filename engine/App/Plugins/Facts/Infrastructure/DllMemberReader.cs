namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;

sealed class DllMemberReader {
    const string Indexer = "this[]";
    const int TrivialConstructorSize = 7;
    const string ParamArray = "System.ParamArrayAttribute";

    readonly Type type;
    readonly DllContext context;
    readonly int fallbackLine;
    readonly ILookup<int, TypeName> implements;

    public DllMemberReader(Type type, DllContext context, int fallbackLine) {
        this.type = type;
        this.context = context;
        this.fallbackLine = fallbackLine;
        implements = new Implementations(type, context).Read();
    }

    public IReadOnlyList<MethodInfo> Accessors => [.. Properties().SelectMany(AccessorsOf).Concat(Events().SelectMany(AccessorsOf))];

    public IEnumerable<DllMember> Read() {
        var accessors = new HashSet<int>(Accessors.Select(m => m.MetadataToken));
        var events = new HashSet<string>(Events().Select(e => e.Name), StringComparer.Ordinal);
        return Fields().Where(f => !events.Contains(f.Name)).Select(Field)
            .Concat(Properties().Select(Property))
            .Concat(Events().Select(Event))
            .Concat(Constructors().Where(IsWritten).Select(Constructor))
            .Concat(Methods().Where(m => !accessors.Contains(m.MetadataToken)).Select(Method));
    }

    public IEnumerable<string> Mutable(bool isEnum) {
        if (isEnum) {
            return [];
        }
        var events = Events().Where(e => Fields().Any(f => f.Name == e.Name)).Select(e => e.Name).ToList();
        return Fields().Where(f => !f.IsInitOnly && !f.IsLiteral && !events.Contains(f.Name)).Select(f => f.Name)
            .Concat(events)
            .Concat(Properties().Where(p => p.GetSetMethod(true) is { } set && !ReflectionFlags.IsInitOnly(set)).Select(p => p.Name))
            .Distinct();
    }

    public IEnumerable<FieldInfo> Fields() =>
        ReflectionFlags.Safe(() => type.GetFields(ReflectionFlags.Declared), [])
            .Where(f => !ReflectionFlags.IsCompilerName(f.Name) && !f.IsSpecialName);

    public IEnumerable<MethodInfo> Methods() =>
        ReflectionFlags.Safe(() => type.GetMethods(ReflectionFlags.Declared), []).Where(m => !ReflectionFlags.IsCompilerName(m.Name));

    PropertyInfo[] Properties() => ReflectionFlags.Safe(() => type.GetProperties(ReflectionFlags.Declared), []);

    EventInfo[] Events() => ReflectionFlags.Safe(() => type.GetEvents(ReflectionFlags.Declared), []);

    IEnumerable<ConstructorInfo> Constructors() =>
        ReflectionFlags.Safe(() => type.GetConstructors(ReflectionFlags.Declared), []).Where(c => !c.IsStatic);

    static IEnumerable<MethodInfo> AccessorsOf(PropertyInfo property) =>
        new[] { property.GetGetMethod(true), property.GetSetMethod(true) }.OfType<MethodInfo>();

    static IEnumerable<MethodInfo> AccessorsOf(EventInfo declared) =>
        new[] { declared.GetAddMethod(true), declared.GetRemoveMethod(true), declared.GetRaiseMethod(true) }.OfType<MethodInfo>();

    bool IsWritten(ConstructorInfo constructor) =>
        ReflectionFlags.Safe(() => constructor.GetParameters().Length, 0) > 0
        || context.Symbols.BodyOf(Handle(constructor)) is not { Size: <= TrivialConstructorSize };

    DllMember Field(FieldInfo field) => new(
        field.Name, DllMemberKind.Field, ReflectionAccess.Of(field),
        new DllMemberTraits(field.IsStatic, false, false, false, false, field.IsInitOnly, field.IsLiteral, false,
            ReflectionFlags.IsCompilerMade(field)),
        fallbackLine, Shape(() => field.FieldType));

    DllMember Property(PropertyInfo property) {
        var get = property.GetGetMethod(true);
        var set = property.GetSetMethod(true);
        var any = get ?? set!;
        var parameters = ReflectionFlags.Safe(() => property.GetIndexParameters(), []);
        return new DllMember(
            parameters.Length > 0 ? Indexer : property.Name, DllMemberKind.Property, ReflectionAccess.Widest([get, set]),
            Traits(any) with { IsReadOnly = set is null, IsImplicit = ReflectionFlags.IsCompilerMade(property) },
            LineOf(any), Shape(() => property.PropertyType)) {
            Parameters = [.. parameters.Select(Parameter)],
            Implements = [.. AccessorsOf(property).SelectMany(m => implements[m.MetadataToken]).Distinct()],
            Get = get is null ? null : ReflectionAccess.Of(get),
            Set = set is null ? null : ReflectionAccess.Of(set),
        };
    }

    DllMember Event(EventInfo declared) {
        var add = declared.GetAddMethod(true)!;
        return new DllMember(
            declared.Name, DllMemberKind.Event, ReflectionAccess.Of(add),
            Traits(add) with { IsImplicit = ReflectionFlags.IsCompilerMade(declared) }, LineOf(add), Shape(() => declared.EventHandlerType!)) {
            Implements = [.. AccessorsOf(declared).SelectMany(m => implements[m.MetadataToken]).Distinct()],
        };
    }

    DllMember Constructor(ConstructorInfo constructor) => new(
        constructor.Name, DllMemberKind.Constructor, ReflectionAccess.Of(constructor), Traits(constructor), LineOf(constructor), null) {
        Parameters = [.. ReflectionFlags.Safe(() => constructor.GetParameters(), []).Select(Parameter)],
    };

    DllMember Method(MethodInfo method) => new(
        method.Name, KindOf(method), ReflectionAccess.Of(method), Traits(method), LineOf(method), Shape(() => method.ReturnType)) {
        Parameters = [.. ReflectionFlags.Safe(() => method.GetParameters(), []).Select(Parameter)],
        TypeParameters = method.IsGenericMethodDefinition ? [.. method.GetGenericArguments().Select(DllTypeParameters.Of)] : [],
        Implements = [.. implements[method.MetadataToken].Distinct()],
    };

    static DllMemberKind KindOf(MethodInfo method) => method.Name switch {
        "Finalize" when method.IsFamily && ReflectionFlags.Safe(() => method.GetParameters().Length, 1) == 0 => DllMemberKind.Destructor,
        _ when method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal) => DllMemberKind.Operator,
        _ => DllMemberKind.Method,
    };

    static DllMemberTraits Traits(MethodBase method) {
        var isOverride = method.IsVirtual && !method.Attributes.HasFlag(MethodAttributes.NewSlot);
        var isInterface = method.DeclaringType?.IsInterface == true;
        return new DllMemberTraits(
            method.IsStatic, method.IsAbstract,
            method.IsVirtual && !isOverride && !method.IsAbstract && !method.IsFinal && !isInterface,
            isOverride, isOverride && method.IsFinal, ReflectionFlags.IsReadOnly(method), false, ReflectionFlags.IsAsync(method),
            ReflectionFlags.IsCompilerMade(method));
    }

    static DllParameter Parameter(ParameterInfo parameter) {
        var type = ReflectionFlags.Safe<Type?>(() => parameter.ParameterType, null);
        var passing = type is not { IsByRef: true } ? null : parameter.IsIn ? "in" : parameter.IsOut ? "out" : "ref";
        var isParams = ReflectionFlags.Safe(() => parameter.GetCustomAttributesData()
            .Any(a => a.AttributeType.FullName == ParamArray), false);
        return new DllParameter(
            parameter.Name ?? "", (type is null ? null : Shape(() => type)) ?? new DllShape("", []), parameter.IsOptional, isParams, passing);
    }

    static DllShape? Shape(Func<Type> read) => ReflectionFlags.Safe<DllShape?>(() => ReflectionShapes.Of(read()), null);

    static System.Reflection.Metadata.MethodDefinitionHandle Handle(MethodBase method) =>
        System.Reflection.Metadata.Ecma335.MetadataTokens.MethodDefinitionHandle(method.MetadataToken);

    int LineOf(MethodBase method) => context.Symbols.PointsOf(Handle(method)).Select(p => p.Line).DefaultIfEmpty(fallbackLine).First();
}
