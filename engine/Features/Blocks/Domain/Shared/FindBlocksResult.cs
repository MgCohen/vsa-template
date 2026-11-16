namespace Blocks.Domain.Shared;

using Steps.Domain.Shared;

public sealed record FindBlocksResult(IReadOnlyList<Finding> Findings, BlockTree Blocks) : IStepResult;
