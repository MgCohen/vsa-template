namespace Blocks.Domain.Shared;

using Facts.Domain.Shared;
using Operations.Domain.Shared;

public sealed record FindBlocksResult(IReadOnlyList<Finding> Findings, BlockTree Blocks, BinIndex Bin) : IOperationResult;
