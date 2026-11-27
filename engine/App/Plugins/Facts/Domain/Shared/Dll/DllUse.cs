namespace Facts.Domain.Shared.Dll;

public sealed record DllUse(TypeName Target, DllUseKind Kind, string File, int Line, int Column) {
    public string? Member { get; init; }
    public string? TargetMember { get; init; }
}
