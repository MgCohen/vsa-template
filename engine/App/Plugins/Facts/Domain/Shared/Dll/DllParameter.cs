namespace Facts.Domain.Shared.Dll;

public sealed record DllParameter(string Name, DllShape Type, bool IsOptional, bool IsParams, string? Passing);
