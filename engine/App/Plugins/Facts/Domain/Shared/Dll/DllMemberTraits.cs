namespace Facts.Domain.Shared.Dll;

public sealed record DllMemberTraits(
    bool IsStatic,
    bool IsAbstract,
    bool IsVirtual,
    bool IsOverride,
    bool IsSealed,
    bool IsReadOnly,
    bool IsConst,
    bool IsAsync,
    bool IsImplicit);
