namespace Umpire.Architecture;

internal static class KindText {
    public static string Of(TypeKind kinds) => string.Join(" or ", Enum.GetValues(typeof(TypeKind)).Cast<TypeKind>()
        .Where(kind => kind != TypeKind.None && kinds.HasFlag(kind))
        .Select(kind => kind.ToString()));
}
