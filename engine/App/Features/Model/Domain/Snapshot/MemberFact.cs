namespace Umpire.CheckModel;

public sealed record MemberFact(
    string Name,
    MemberKind Kind,
    MemberAccess Access,
    IReadOnlyList<ParameterFact> Parameters,
    IReadOnlyList<TypeParameterFact> TypeParameters,
    IReadOnlyList<TypeName> Implements,
    bool IsStatic,
    bool IsAbstract,
    bool IsVirtual,
    bool IsOverride,
    bool IsSealed,
    bool IsReadOnly,
    bool IsConst,
    bool IsAsync,
    bool IsImplicit,
    int Line,
    TypeShape? Type = null,
    MemberAccess? Get = null,
    MemberAccess? Set = null);
