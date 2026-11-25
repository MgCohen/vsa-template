namespace Umpire.CheckModel;

public sealed record TypeContent(
    TypeKind Kind,
    MemberAccess Access,
    bool IsAbstract,
    bool IsStatic,
    bool IsSealed,
    bool IsRecord,
    bool IsPartial,
    bool IsReadOnly,
    bool IsRefLike,
    IReadOnlyList<TypeParameterFact> TypeParameters,
    IReadOnlyList<TypeShape> Interfaces,
    IReadOnlyList<TypeShape> AllInterfaces,
    IReadOnlyList<TypeName> Bases,
    IReadOnlyList<TypeName> BaseArguments,
    IReadOnlyList<AttributeFact> Attributes,
    IReadOnlyList<MemberFact> Members,
    TypeShape? Base = null);
