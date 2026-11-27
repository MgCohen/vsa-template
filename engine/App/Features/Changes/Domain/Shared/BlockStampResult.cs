namespace Changes.Domain.Shared;

using Operations.Domain.Shared;

public sealed record BlockStampResult(IReadOnlyList<Finding> Findings, bool Same, string File, string Hash);
