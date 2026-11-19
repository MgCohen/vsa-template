namespace Blocks.Slices.FindBlocks;

using Blocks.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record FindBlocksCommand(string Project, string Bin, string Declared)
    : EngineStep<FindBlocksResult>, IStepGroup, IStepStage {
    public static int Group => 3;
    public static Stage Stages => Stage.Check;
}
