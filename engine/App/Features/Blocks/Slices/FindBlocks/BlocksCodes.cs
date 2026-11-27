namespace Blocks.Slices.FindBlocks;

using Operations.Domain.Shared;

public static class BlocksCodes {
    public static readonly string TwoEntries = OperationCode.Of<FindBlocksCommand>(1);
    public static readonly string BadArchitecture = OperationCode.Of<FindBlocksCommand>(2);
    public static readonly string NoBlock = OperationCode.Of<FindBlocksCommand>(3);
    public static readonly string CodeWins = OperationCode.Of<FindBlocksCommand>(4);
    public static readonly string NoUmpirePackage = OperationCode.Of<FindBlocksCommand>(5);
    public static readonly string NoConfigFile = OperationCode.Of<FindBlocksCommand>(6);
    public static readonly string NewerRuntime = OperationCode.Of<FindBlocksCommand>(7);
    public static readonly string NoDefinitionProject = OperationCode.Of<FindBlocksCommand>(8);
}
