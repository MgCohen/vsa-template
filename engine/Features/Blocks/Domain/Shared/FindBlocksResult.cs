namespace Blocks.Domain.Shared;

using BuiltApp.Domain.Shared;
using Steps.Domain.Shared;

public sealed record FindBlocksResult(IReadOnlyList<Finding> Findings, BlockTree Blocks, BinIndex Bin) : IStepResult;
