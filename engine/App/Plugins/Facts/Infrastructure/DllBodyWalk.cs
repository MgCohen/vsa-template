namespace Facts.Infrastructure;

using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Facts.Domain.Shared.Dll;

sealed class DllBodyWalk(DllContext context, UseList uses, IReadOnlyList<MethodInfo> accessors) {
    const string Constructor = ".ctor";

    public void Walk(Type type) {
        foreach (var method in Bodies(type)) {
            Read(method, MemberOf(method, null));
        }
        foreach (var nested in Hidden(type)) {
            WalkHidden(nested, NameIn(nested.Name));
        }
    }

    void WalkHidden(Type type, string? member) {
        foreach (var field in ReflectionFlags.Safe(() => type.GetFields(ReflectionFlags.Declared), [])) {
            var shape = ReflectionFlags.Safe<DllShape?>(() => ReflectionShapes.Of(field.FieldType), null);
            uses.Add(shape?.Types ?? [], DllUseKind.Inferred, 0, member);
        }
        foreach (var method in Bodies(type)) {
            Read(method, NameIn(method.Name) ?? member);
        }
        foreach (var nested in Hidden(type)) {
            WalkHidden(nested, NameIn(nested.Name) ?? member);
        }
    }

    static IEnumerable<MethodBase> Bodies(Type type) =>
        ReflectionFlags.Safe(() => type.GetMethods(ReflectionFlags.Declared), []).Cast<MethodBase>()
            .Concat(ReflectionFlags.Safe(() => type.GetConstructors(ReflectionFlags.Declared), []))
            .Where(m => ReflectionFlags.IsCompilerName(m.Name) || !ReflectionFlags.IsCompilerMade(m));

    static IEnumerable<Type> Hidden(Type type) =>
        ReflectionFlags.Safe(() => type.GetNestedTypes(ReflectionFlags.Declared), []).Where(t => ReflectionFlags.IsCompilerName(t.Name));

    void Read(MethodBase method, string? member) {
        var handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken);
        uses.Add(context.Bodies.Of(handle), context.Symbols.PointsOf(handle), member);
    }

    string? MemberOf(MethodBase method, string? fallback) {
        if (method is ConstructorInfo) {
            return Constructor;
        }
        if (accessors.FirstOrDefault(a => a.MetadataToken == method.MetadataToken) is { } accessor) {
            var underscore = accessor.Name.IndexOf('_');
            return underscore < 0 ? accessor.Name : accessor.Name.Substring(underscore + 1);
        }
        return NameIn(method.Name) ?? (ReflectionFlags.IsCompilerName(method.Name) ? fallback : method.Name);
    }

    static string? NameIn(string name) {
        var end = name.IndexOf('>');
        return name.StartsWith("<", StringComparison.Ordinal) && end > 1 ? name.Substring(1, end - 1) : null;
    }
}
