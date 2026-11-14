namespace VSA.Architecture;

public enum Access { Private, PrivateProtected, Protected, Internal, ProtectedInternal, Public }

public static class AccessOrder {
    public static bool IsWithin(this Access access, Access limit) =>
        InAssembly(access) <= InAssembly(limit) && OutsideAssembly(access) <= OutsideAssembly(limit);

    public static Access Narrowest(this Access first, Access second) =>
        From(Math.Min(InAssembly(first), InAssembly(second)), Math.Min(OutsideAssembly(first), OutsideAssembly(second)));

    static int InAssembly(Access access) => access switch {
        Access.Private => 0,
        Access.PrivateProtected or Access.Protected => 1,
        _ => 2,
    };

    static int OutsideAssembly(Access access) => access switch {
        Access.Public => 2,
        Access.Protected or Access.ProtectedInternal => 1,
        _ => 0,
    };

    static Access From(int inAssembly, int outsideAssembly) => (inAssembly, outsideAssembly) switch {
        (2, 2) => Access.Public,
        (2, 1) => Access.ProtectedInternal,
        (2, _) => Access.Internal,
        (1, 1) => Access.Protected,
        (1, _) => Access.PrivateProtected,
        _ => Access.Private,
    };
}
