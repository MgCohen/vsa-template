namespace Facts.Infrastructure;

using System.Reflection;
using Facts.Domain.Shared.Dll;

static class ReflectionAttributes {
    const string ObjectType = "System.Object";
    static readonly string[] CompilerSpaces = ["System.Runtime.CompilerServices", "Microsoft.CodeAnalysis"];
    static readonly string[] CompilerTypes = [
        "System.ParamArrayAttribute", "System.Diagnostics.DebuggerStepThroughAttribute", "System.Diagnostics.DebuggerHiddenAttribute",
        "System.Diagnostics.DebuggerBrowsableAttribute", "System.Diagnostics.DebuggerNonUserCodeAttribute",
        "System.Reflection.DefaultMemberAttribute",
    ];

    public static IEnumerable<DllAttributeFact> Of(IEnumerable<CustomAttributeData> attributes, string? member) =>
        attributes.Select(attribute => Of(attribute, member)).OfType<DllAttributeFact>();

    public static IEnumerable<CustomAttributeData> On(MemberInfo member) {
        var own = ReflectionFlags.Safe<IEnumerable<CustomAttributeData>>(member.GetCustomAttributesData, []);
        return member switch {
            MethodInfo method => own.Concat(OnMethod(method)),
            ConstructorInfo constructor => own.Concat(OnParameters(constructor)),
            PropertyInfo property => own.Concat(ReflectionFlags.Safe(() => property.GetIndexParameters(), []).SelectMany(OnParameter)),
            _ => own,
        };
    }

    public static IEnumerable<CustomAttributeData> OnParameter(ParameterInfo parameter) =>
        ReflectionFlags.Safe<IEnumerable<CustomAttributeData>>(parameter.GetCustomAttributesData, []);

    static IEnumerable<CustomAttributeData> OnMethod(MethodInfo method) =>
        ReflectionFlags.Safe(() => OnParameter(method.ReturnParameter).ToList(), [])
            .Concat(method.IsGenericMethodDefinition ? method.GetGenericArguments().SelectMany(On) : [])
            .Concat(OnParameters(method));

    static IEnumerable<CustomAttributeData> OnParameters(MethodBase method) =>
        ReflectionFlags.Safe(() => method.GetParameters(), []).SelectMany(OnParameter);

    static DllAttributeFact? Of(CustomAttributeData attribute, string? member) {
        Type type;
        try {
            type = attribute.AttributeType;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return null;
        }
        if (IsCompilerMade(type)) {
            return null;
        }
        return new DllAttributeFact(
            ReflectionShapes.NameOf(type),
            [.. ReflectionFlags.Safe(() => Bases(type).ToList(), [type]).Select(ReflectionShapes.NameOf)],
            [.. ReflectionFlags.Safe(() => Arguments(attribute).ToList(), [])],
            member);
    }

    static bool IsCompilerMade(Type type) =>
        CompilerSpaces.Contains(type.Namespace) || CompilerTypes.Contains(type.FullName);

    static IEnumerable<Type> Bases(Type type) {
        for (var current = type; current is not null && current.FullName != ObjectType; current = current.BaseType) {
            yield return current;
        }
    }

    static IEnumerable<string> Arguments(CustomAttributeData attribute) =>
        attribute.ConstructorArguments.Select(Text)
            .Concat(attribute.NamedArguments.Select(named => $"{named.MemberName} = {Text(named.TypedValue)}"));

    static string Text(CustomAttributeTypedArgument argument) => argument.Value switch {
        null => "null",
        string text => $"\"{text}\"",
        Type type => $"typeof({ReflectionShapes.FullName(type)})",
        bool flag => flag ? "true" : "false",
        _ => Convert.ToString(argument.Value, System.Globalization.CultureInfo.InvariantCulture) ?? "",
    };
}
