namespace Build.Slices.BuildApp;

using Build.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record BuildAppCommand(string Project) : EngineStep<BuildAppResult>, IStepGroup {
    public static int Group => 2;
}
