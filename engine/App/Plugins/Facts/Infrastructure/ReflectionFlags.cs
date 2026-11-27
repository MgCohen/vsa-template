namespace Facts.Infrastructure;

using System.Reflection;

static class ReflectionFlags {
    public const BindingFlags Declared =
        BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    const string CompilerName = "<";
    const string CompilerGenerated = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    const string ReadOnly = "System.Runtime.CompilerServices.IsReadOnlyAttribute";
    const string Async = "System.Runtime.CompilerServices.AsyncStateMachineAttribute";
    const string InitOnly = "System.Runtime.CompilerServices.IsExternalInit";

    public static bool IsCompilerName(string name) => name.StartsWith(CompilerName, StringComparison.Ordinal);

    public static bool IsCompilerMade(MemberInfo member) => Has(member, CompilerGenerated);

    public static bool IsReadOnly(MemberInfo member) => Has(member, ReadOnly);

    public static bool IsAsync(MemberInfo member) => Has(member, Async);

    public static bool IsInitOnly(MethodInfo setter) {
        try {
            return setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.FullName == InitOnly);
        } catch (Exception e) when (e is NotSupportedException or FileNotFoundException or TypeLoadException) {
            return false;
        }
    }

    public static bool Has(MemberInfo member, string attribute) => Names(member).Contains(attribute);

    public static IEnumerable<string> Names(MemberInfo member) {
        try {
            return [.. member.GetCustomAttributesData().Select(NameOf).OfType<string>()];
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return [];
        }
    }

    public static T Safe<T>(Func<T> read, T fallback) {
        try {
            return read();
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException or NotSupportedException) {
            return fallback;
        }
    }

    static string? NameOf(CustomAttributeData attribute) {
        try {
            return attribute.AttributeType.FullName;
        } catch (Exception e) when (e is FileNotFoundException or FileLoadException or TypeLoadException) {
            return null;
        }
    }
}
