namespace Facts.Infrastructure;

using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;

sealed record BodyUse(TypeName Target, DllUseKind Kind, int Offset, string? TargetMember);
