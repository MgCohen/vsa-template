namespace Blocks.Slices.FindBlocks;

using Steps.Domain.Shared;

public static class BlocksCodes {
    public static readonly string TwoEntries = StepCode.Of<FindBlocksCommand>(1);
    public static readonly string BadArchitecture = StepCode.Of<FindBlocksCommand>(2);
    public static readonly string NoBlock = StepCode.Of<FindBlocksCommand>(3);
    public static readonly string CodeWins = StepCode.Of<FindBlocksCommand>(4);
    public static readonly string NoUmpireSdk = StepCode.Of<FindBlocksCommand>(5);
    public static readonly string NoConfigFile = StepCode.Of<FindBlocksCommand>(6);
    public static readonly string NewerRuntime = StepCode.Of<FindBlocksCommand>(7);
    public static readonly string NoDefinitionProject = StepCode.Of<FindBlocksCommand>(8);
}
