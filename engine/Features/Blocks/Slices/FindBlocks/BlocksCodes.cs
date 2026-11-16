namespace Blocks.Slices.FindBlocks;

using Steps.Domain.Shared;

public static class BlocksCodes {
    public static readonly string TwoEntries = StepCode.Of<FindBlocksCommand>(1);
    public static readonly string BadArchitecture = StepCode.Of<FindBlocksCommand>(2);
    public static readonly string NoBlock = StepCode.Of<FindBlocksCommand>(3);
}
