namespace Build.Slices.BuildApp;

using Build.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record BuildAppCommand(string Project, string Bin)
    : EngineStep<BuildAppResult>, IStepGroup, IStepStage {
    public static int Group => 1;
    public static Stage Stages => Stage.Build;
}
