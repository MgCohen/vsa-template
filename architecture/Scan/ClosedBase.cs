namespace Umpire.Architecture;

public static class ClosedBase {
    public static Type? Of(Type type, Type definition) {
        for (var current = type; current is not null; current = current.BaseType) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == definition) {
                return current;
            }
        }
        return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == definition);
    }
}
