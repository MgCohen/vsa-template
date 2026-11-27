namespace Operations.Domain.Shared;

public sealed record FindingCode(string Code, string Title, bool AtEnd, bool IsCodeRule = false);
