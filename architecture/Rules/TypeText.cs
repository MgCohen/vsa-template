namespace Umpire.Architecture;

internal static class TypeText {
    public static string Of(Type type) => type.Name.Split('`')[0];

    public static string OpenName(Type type) => (type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName!;
}
