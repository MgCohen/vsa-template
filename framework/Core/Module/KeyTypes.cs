namespace VSA.Module;

public static class KeyTypes {
    public const string MarkerName = "VSA.Analyzers.Markers.KeyAttribute";

    public static bool IsKey(Type type) =>
        !type.ContainsGenericParameters
        && (type.IsValueType || type is { IsClass: true, IsAbstract: false })
        && (HasMarker(type) || type.GetInterfaces().Any(HasMarker));

    static bool HasMarker(Type type) {
        for (var t = type; t is not null; t = t.BaseType) {
            if (t.GetCustomAttributesData().Any(a => a.AttributeType.FullName == MarkerName)) {
                return true;
            }
        }
        return false;
    }
}
