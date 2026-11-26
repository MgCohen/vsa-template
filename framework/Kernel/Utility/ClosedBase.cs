namespace Umpire.Utility;

public static class ClosedBase {
    public static Type? Of(Type type, Type definition) =>
        type.BaseType is { } baseType ? OfSelfOrBase(baseType, definition) : null;

    public static Type? OfSelfOrBase(Type type, Type definition) {
        for (var current = type; current is not null; current = current.BaseType) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == definition) {
                return current;
            }
        }
        return null;
    }

    public static Type? InterfaceOf(Type type, Type definition) =>
        type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == definition);
}
