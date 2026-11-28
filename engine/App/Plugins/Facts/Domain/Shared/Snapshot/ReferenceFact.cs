namespace Umpire.CheckModel;

public sealed record ReferenceFact(
    TypeName Target, ReferenceKind Kind, int Line, int Column, string? Member = null, string? TargetMember = null);
