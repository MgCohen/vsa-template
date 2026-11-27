namespace Operations.Domain.Shared;

public sealed record Finding(
    string Code, FindingSeverity Severity, string Message, string? File = null, int? Line = null, int? Column = null);
