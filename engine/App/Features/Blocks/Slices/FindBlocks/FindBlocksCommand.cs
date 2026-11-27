namespace Blocks.Slices.FindBlocks;

using Blocks.Domain.Shared;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record FindBlocksCommand(string Project, string Bin, string Declared, bool IsUsed = false)
    : JobOperation<FindBlocksResult>, IOperationGroup {
    public static int Group => 3;
}
