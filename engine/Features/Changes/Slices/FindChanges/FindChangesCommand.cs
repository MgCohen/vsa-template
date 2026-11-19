namespace Changes.Slices.FindChanges;

using Changes.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record FindChangesCommand(string Block, string Bin, string Run)
    : EngineStep<ChangesResult>, IStepGroup, IStepStage {
    public static int Group => 2;
    public static Stage Stages => Stage.Check;
}
